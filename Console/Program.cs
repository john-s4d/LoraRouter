using LoraRouter.ChirpStack;
using LoraRouter.SemtechUdp;

namespace LoraRouter
{
    internal class Program
    {
        private static Logger _logger = new Logger();

        private static void Main(string[] args)
        {
            // Logger
            _logger.Config[LoggerConfig.LOG_FILE] = @"C:\Users\john\Documents\Helium\Lora_Router_log.txt";
            _logger.EventLogged += _logger_EventLogged;

            // ChirpStack Bridge
            ChirpstackBridge csBridge = new ChirpstackBridge(_logger);
            csBridge.Config.MqttConfig[MqttConfig.HOST] = "a3onl3zll958hs-ats.iot.us-east-1.amazonaws.com";
            csBridge.Config.MqttConfig[MqttConfig.PORT] = 8883;
            csBridge.Config.MqttConfig[MqttConfig.CLIENT_ID] = "LoraRouter";
            csBridge.Config.MqttConfig[MqttConfig.CA_CERT_FILE] = @"C:\Users\john\Documents\Helium\Keys\AmazonRootCA1.pem";
            csBridge.Config.MqttConfig[MqttConfig.CLIENT_CERT_FILE] = @"C:\Users\john\Documents\Helium\Keys\LoraRouter.cert.pfx";

            // Semtech UDP Bridge
            SemtechUdpBridge suBridge = new SemtechUdpBridge(_logger);
            suBridge.Config[SemtechUdpConfig.PORT] = 1700;
                        
            // Lora Basic Station
            // TODO: Implement            

            // Gateways
            Gateway gateway0 = new Gateway() { Id = "a84041fffe2188a0", PowerMode = TxPowerMode.MAXIMUM, TxFrequencies = { 903.9F, 904.1F } };
            Gateway gateway1 = new Gateway() { Id = "a84041fffe218d24", PowerMode = TxPowerMode.RANDOMIZE, TxFrequencies = { 903.9F, 904.1F, 904.3F, 904.5F } };
            Gateway gateway2 = new Gateway() { Id = "a84041fffe21c9b4", PowerMode = TxPowerMode.RANDOMIZE, TxFrequencies = { 904.7F, 904.9F, 905.1F, 905.3F } };

            // Lora Router 
            LoraRouter router = new LoraRouter(_logger);

            //router.AddBridgeWithGateways(csBridge, new Gateway[] { gateway0, gateway1, gateway2 });
            router.AddBridgeWithGateways(suBridge, new Gateway[] { gateway0, gateway1, gateway2 });

            router.AddRoute("a84041fffe2188a0", "a84041fffe218d24");
            router.AddRoute("a84041fffe2188a0", "a84041fffe21c9b4");
            router.AddRoute("a84041fffe218d24", "a84041fffe2188a0");

            router.Start();

            WaitForEscape();

            router.Stop();
        }

        private static void _logger_EventLogged(object sender, LogEventArgs e)
        {
            Console.WriteLine(e.Level + " | " + e.Message);
        }

        private static void WaitForEscape()
        {
            while (!Console.KeyAvailable && Console.ReadKey(true).Key != ConsoleKey.Escape) { }
        }
    }
}


