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

        private const string MQTT_BROKER_ADDRESS = "blurerobotmqtt.northeurope.cloudapp.azure.com";

        private static Socket _socket;

        static void Main(string[] args)
        {
            string clientId = "BlueRobotSimulatorGateway"; 
            string password = "BlueRobotCompanyForTheWin_2018";
            var username = "service";
            
            // create client instance
            MqttClient client = new MqttClient(MQTT_BROKER_ADDRESS);
            
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
            //fex. moveRobot 
            switch(command.Name)
            {
                case "moveRobot":
                    //_socket.Emit("moveRobot",  new { x= command.Value.X, y=command.Value.Y , z=command.Value.Z });

                    _socket.Emit("moveRobot", JObject.FromObject(new { x= command.Value.X, y=command.Value.Y , z=command.Value.Z }));
                break;
                case "placeBox":
                    _socket.Emit("placeBox", new { X= command.Value.X, Y=command.Value.Y , Z=command.Value.Z });
                break;
                case "pickupBox":
                    _socket.Emit("pickupBox", JObject.FromObject(new { x= command.Value.X, y=command.Value.Y , z=command.Value.Z }));
                break;
                default:
                break;
            }

        }
    }
}
