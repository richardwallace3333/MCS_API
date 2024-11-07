public class MCSMessage
{
    public MCSCommand Command { get; set; }
    public string Parameter1 { get; set; }
    public string Parameter2 { get; set; }

    // You can also add methods to serialize to/from JSON if needed.
}
