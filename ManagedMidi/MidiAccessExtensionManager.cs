namespace ManagedMidi;

public class MidiAccessExtensionManager
{
    public virtual bool Supports<T>() where T : class => GetInstance<T>() != default(T);
    public virtual T GetInstance<T>() where T : class => null;
}
