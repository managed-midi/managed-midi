using System.Runtime.InteropServices;

namespace ManagedMidi;

/// <summary>
/// Static class providing access to default (platform-specific) and empty implementations of <see cref="IMidiAccess"/>.
/// </summary>
public static class MidiAccessManager
{
    public static IMidiAccess Default => DefaultImpl.Default;
    public static IMidiAccess Empty { get; } = new EmptyMidiAccess();

    private static class DefaultImpl
    {
        internal static IMidiAccess Default { get; } =
            Environment.OSVersion.Platform != PlatformID.Unix ? (IMidiAccess) new WinMM.WinMMMidiAccess()
            : IsRunningOnMac() ? (IMidiAccess) new CoreMidi.CoreMidiAccess()
            : new Alsa.AlsaMidiAccess();

        //From Managed.Windows.Forms/XplatUI
        [DllImport("libc")]
        static extern int uname(IntPtr buf);

        static bool IsRunningOnMac()
        {
            IntPtr buf = IntPtr.Zero;
            try
            {
                buf = Marshal.AllocHGlobal(8192);
                // This is a hacktastic way of getting sysname from uname ()
                if (uname(buf) == 0)
                {
                    string os = Marshal.PtrToStringAnsi(buf);
                    if (os == "Darwin")
                        return true;
                }
            }
            catch
            {
            }
            finally
            {
                if (buf != IntPtr.Zero)
                    Marshal.FreeHGlobal(buf);
            }
            return false;
        }
    }
}

public interface IMidiAccess
{
    IEnumerable<IMidiPortDetails> Inputs { get; }
    IEnumerable<IMidiPortDetails> Outputs { get; }

    Task<IMidiInput> OpenInputAsync(string portId);
    Task<IMidiOutput> OpenOutputAsync(string portId);
    MidiAccessExtensionManager ExtensionManager { get; }
}

#region draft API

public class MidiAccessExtensionManager
{
    public virtual bool Supports<T>() where T : class => GetInstance<T>() != default(T);

    public virtual T GetInstance<T>() where T : class => null;
}

public abstract class MidiPortCreatorExtension
{
    public abstract IMidiOutput CreateVirtualInputSender(PortCreatorContext context);
    public abstract IMidiInput CreateVirtualOutputReceiver(PortCreatorContext context);

    public delegate void SendDelegate(byte[] buffer, int index, int length, long timestamp);

    public class PortCreatorContext
    {
        public string ApplicationName { get; set; }
        public string PortName { get; set; }
        public string Manufacturer { get; set; }
        public string Version { get; set; }
    }
}

public abstract class SimpleVirtualMidiPort : IMidiPort
{
    IMidiPortDetails details;
    Action on_dispose;
    MidiPortConnectionState connection;

    protected SimpleVirtualMidiPort(IMidiPortDetails details, Action onDispose)
    {
        this.details = details;
        on_dispose = onDispose;
        connection = MidiPortConnectionState.Open;
    }

    public IMidiPortDetails Details => details;

    public MidiPortConnectionState Connection => connection;

    public Task CloseAsync()
    {
        return Task.Run(() =>
        {
            if (on_dispose != null)
                on_dispose();
            connection = MidiPortConnectionState.Closed;
        });
    }

    public void Dispose()
    {
        CloseAsync().Wait();
    }
}

public class SimpleVirtualMidiInput : SimpleVirtualMidiPort, IMidiInput
{
    public SimpleVirtualMidiInput(IMidiPortDetails details, Action onDispose)
        : base(details, onDispose)
    {
    }

    event EventHandler<MidiReceivedEventArgs> IMidiInput.MessageReceived
    {
        add { }
        remove { }
    }
}

public class SimpleVirtualMidiOutput : SimpleVirtualMidiPort, IMidiOutput
{
    public SimpleVirtualMidiOutput(IMidiPortDetails details, Action onDispose)
    : base(details, onDispose)
    {
    }

    public MidiPortCreatorExtension.SendDelegate OnSend { get; set; }

    public void Send(byte[] mevent, int offset, int length, long timestamp)
    {
        if (OnSend != null)
            OnSend(mevent, offset, length, timestamp);
    }
}

#endregion

public class MidiConnectionEventArgs : EventArgs
{
    public IMidiPortDetails Port { get; private set; }
}

public interface IMidiPortDetails
{
    string Id { get; }
    string Manufacturer { get; }
    string Name { get; }
    string Version { get; }
}

public enum MidiPortConnectionState
{
    Open,
    Closed,
    Pending
}

public interface IMidiPort
{
    IMidiPortDetails Details { get; }
    MidiPortConnectionState Connection { get; }
    Task CloseAsync();
}

public interface IMidiInput : IMidiPort, IDisposable
{
    event EventHandler<MidiReceivedEventArgs> MessageReceived;
}

public interface IMidiOutput : IMidiPort, IDisposable
{
    void Send(byte[] mevent, int offset, int length, long timestamp);
}

public class MidiReceivedEventArgs : EventArgs
{
    public long Timestamp { get; set; }
    public byte[] Data { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
}

class EmptyMidiAccess : IMidiAccess
{
    public IEnumerable<IMidiPortDetails> Inputs
    {
        get { yield return EmptyMidiInput.Instance.Details; }
    }

    public IEnumerable<IMidiPortDetails> Outputs
    {
        get { yield return EmptyMidiOutput.Instance.Details; }
    }

    public MidiAccessExtensionManager ExtensionManager { get; } = new();

    public Task<IMidiInput> OpenInputAsync(string portId)
    {
        if (portId != EmptyMidiInput.Instance.Details.Id)
            throw new ArgumentException(string.Format("Port ID {0} does not exist.", portId));
        return Task.FromResult<IMidiInput>(EmptyMidiInput.Instance);
    }

    public Task<IMidiOutput> OpenOutputAsync(string portId)
    {
        if (portId != EmptyMidiOutput.Instance.Details.Id)
            throw new ArgumentException(string.Format("Port ID {0} does not exist.", portId));
        return Task.FromResult<IMidiOutput>(EmptyMidiOutput.Instance);
    }
}

abstract class EmptyMidiPort : IMidiPort
{
    Task completed_task = Task.FromResult(false);

    public IMidiPortDetails Details
    {
        get { return CreateDetails(); }
    }
    internal abstract IMidiPortDetails CreateDetails();

    public MidiPortConnectionState Connection { get; private set; }

    public Task CloseAsync()
    {
        // do nothing.
        return completed_task;
    }

    public void Dispose()
    {
    }
}

class EmptyMidiPortDetails : IMidiPortDetails
{
    public EmptyMidiPortDetails(string id, string name)
    {
        Id = id;
        Manufacturer = "dummy project";
        Name = name;
        Version = "0.0";
    }

    public string Id { get; set; }
    public string Manufacturer { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
}

class EmptyMidiInput : EmptyMidiPort, IMidiInput
{
    static EmptyMidiInput()
    {
        Instance = new EmptyMidiInput();
    }

    public static EmptyMidiInput Instance { get; private set; }

#pragma warning disable 0067
    // will never be fired.
    public event EventHandler<MidiReceivedEventArgs> MessageReceived;
#pragma warning restore 0067

    internal override IMidiPortDetails CreateDetails()
    {
        return new EmptyMidiPortDetails("dummy_in", "Dummy MIDI Input");
    }
}

class EmptyMidiOutput : EmptyMidiPort, IMidiOutput
{
    Task completed_task = Task.FromResult(false);

    static EmptyMidiOutput()
    {
        Instance = new EmptyMidiOutput();
    }

    public static EmptyMidiOutput Instance { get; private set; }

    public void Send(byte[] mevent, int offset, int length, long timestamp)
    {
        // do nothing.
    }

    internal override IMidiPortDetails CreateDetails()
    {
        return new EmptyMidiPortDetails("dummy_out", "Dummy MIDI Output");
    }
}
