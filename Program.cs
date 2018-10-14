using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quobject.SocketIoClientDotNet.Client;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace BlueRobotMqttGateway
{
   
    class Program
    {
        private int xMin = -44;
        private int xMax = 44;
        private int yMax = 54;
        private int yMin = -46;

        private int zMax = -1;
        private int zMin = -94;

        private const string MQTT_BROKER_ADDRESS = "blurerobotmqtt.northeurope.cloudapp.azure.com";

        private static Socket _socket;

        private static Position currentPosition;
        static MqttClient client;

        static void Main(string[] args)
        {
            currentPosition = new Position(){X=0, Y=0, Z=0};
            string clientId = "BlueRobotSimulatorGateway"; 
            string password = "BlueRobotCompanyForTheWin_2018";
            var username = "service";
            
            // create client instance
            client = new MqttClient(MQTT_BROKER_ADDRESS);
            
            // register to message received
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            
            client.Connect(clientId, username, password); 
            
            // subscribe to the topic "/home/temperature" with QoS 2
            client.Subscribe(new string[] { "Lab/command" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            
            _socket = IO.Socket("http://localhost:8111");
            _socket.Connect();
            _socket.On(Socket.EVENT_CONNECT, () =>
            {
               Console.WriteLine("Connected to Bluerobot Simulator");
                
            });
            _socket.On("eventMessage", (data) =>
            {
                HandleBlueRobotMessage("eventMessage", data.ToString());
               
            });
            
            _socket.On("boxPlaced", (data) =>
            {
                HandleBlueRobotMessage("boxPlaced", data);
               
            });
        
            _socket.On("objectMove", (data) =>
            {
                HandleBlueRobotMessage("objectMove", data);
            });
                              
            _socket.On("objectMoved", (data) =>
            {
                HandleBlueRobotMessage("objectMoved", data);
            });
        }

        private static void HandleBlueRobotMessage(string messageid, object data)
        {
            var receivedPayload = JObject.FromObject(data);
               Console.WriteLine(string.Format($"Received {messageid}"));
               Console.WriteLine(data);

            if(messageid == "objectMoved")
            {
                var moved = JsonConvert.DeserializeObject<ObjectMoved>(data.ToString());
                if(moved.Name == "robot")
                {
                    //Publish current position?
                    //client.Publish("Blue");
                    currentPosition.X = moved.X;
                    currentPosition.Y = moved.Z;
                    currentPosition.Z = moved.Y;
                }
            }
        }

        private static void HandleBlueRobotMessage(string messageid, string data)
        {
               Console.WriteLine(string.Format($"Received {messageid}:"));
               Console.WriteLine(data);
        }

        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            try
            {
                // handle message received
                var bytesAsString = Encoding.UTF8.GetString(e.Message);
                
                var command = JsonConvert.DeserializeObject<Command>(bytesAsString);

                Console.WriteLine("Received command: ", command.ToString());

                HandleCommand(command);
            }
     
            catch (System.Exception ex)
            {
                Console.WriteLine("Error occured when receiving data: ", ex.ToString());
            }
        }

        private static void HandleCommand(Command command)
        {
            _socket.Connect();
            var position = new DevicePosition(){x = currentPosition.X, y = currentPosition.Y, z = currentPosition.Z};

            //fex. moveRobot 
            switch(command.Name)
            {
                case "moveRobot":
                    HandleMoveCommand(command);
                break;
                case "placeBox":
                   _socket.Emit("placeBox",  JObject.FromObject(position));
                break;
                case "pickupBox":
                    _socket.Emit("pickupBox",  JObject.FromObject(position));
                break;
                default:
                break;
            }
        }

        private static void HandleMoveCommand(Command command)
        {
            //Todo: position convert fixup?
           var position = new DevicePosition(){x = currentPosition.X, y = currentPosition.Y, z = currentPosition.Z};

             switch(command.Direction)
            {
                case "left":
                position.x -= command.StepSize;
                break;
                case "up":
                position.y -=  command.StepSize;
                break;
                case "right":
                position.x +=  command.StepSize;         
                break;
                case "down":
                position.y += command.StepSize;          
                break;
            }
            
             _socket.Emit("moveRobot", JObject.FromObject(position));
        }
    }
}
