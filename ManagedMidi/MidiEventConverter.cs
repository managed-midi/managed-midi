namespace ManagedMidi;

/// <summary>
/// A converter for MIDI events. This is stateful as it needs to keep track
/// of the running status.
/// </summary>
public class MidiEventConverter
{
    private byte runningStatus;
    public IEnumerable<MidiEvent> Convert(byte[] bytes, int index, int size)
    {
        int i = index;
        int end = index + size;
        while (i < end)
        {
            if (bytes[i] >= 128)
            {
                runningStatus = bytes[i];
                if (bytes[i] == 0xF0)
                {
                    yield return new MidiEvent(0xF0, 0, 0, bytes, index, size);
                    i += size;
                }
                else
                {
                    var z = MidiEvent.FixedDataSize(bytes[i]);
                    if (end < i + z)
                    {
                        throw new Exception($"Received data was incomplete to build MIDI status message for '{bytes[i]:X}' status.");
                    }
                    yield return new MidiEvent(bytes[i],
                        (byte) (z > 0 ? bytes[i + 1] : 0),
                        (byte) (z > 1 ? bytes[i + 2] : 0),
                        null, 0, 0);
                    i += z + 1;
                }
            }
            else
            {
                var z = MidiEvent.FixedDataSize(runningStatus);
                if (end < i + z - 1)
                {
                    throw new Exception($"Received data was incomplete to build MIDI running status message for '{runningStatus:X}' status.");
                }
                yield return new MidiEvent(runningStatus,
                    bytes[i],
                    (byte) (z > 1 ? bytes[i + 1] : 0),
                    null, 0, 0);
                i += z;
            }
        }
    }

}
