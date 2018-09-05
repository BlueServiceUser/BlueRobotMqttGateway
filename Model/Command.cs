namespace BlueRobotMqttGateway
{
    public class Command
    {
        public string Name { get; set; }

        public Position Value { get; set; }
    }

    public class Position
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
    }
}