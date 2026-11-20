// See https://aka.ms/new-console-template for more information
using IBM.WMQ;
using System.Collections;
using System.Text;

internal class Program
{
    private static void Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        string strQueueManagerName = "ESBQM2";
        string strChannelName = "forcbcard";
        string inputQueueName = "CBCARD.ESB.RQ1";
        string outputQueueName = "CBCARD.ESB.RS1";
        //string strServerName = "SERVER2019";
        string strServerName = "PATSB03";
        int serverCCSID = 437;
        int intPort = 1415;

        Hashtable queueProperties = new Hashtable
            {
                { MQC.HOST_NAME_PROPERTY, strServerName                  },
                { MQC.CHANNEL_PROPERTY, strChannelName                   },
                { MQC.PORT_PROPERTY, intPort                             },
                { MQC.CCSID_PROPERTY,serverCCSID                         },
                { MQC.USER_ID_PROPERTY, "MUSR_MQADMIN"                   },
                { MQC.TRANSPORT_PROPERTY, MQC.TRANSPORT_MQSERIES_MANAGED }
            };

        MQQueueManager qMgr = new MQQueueManager(strQueueManagerName, queueProperties);

        MQGetMessageOptions gmo = new MQGetMessageOptions();
        gmo.Options |= MQC.MQGMO_WAIT | MQC.MQGMO_FAIL_IF_QUIESCING;
        gmo.WaitInterval = 1000; // Wait for 5 seconds

        MQQueue inputQueue = qMgr.AccessQueue(inputQueueName, MQC.MQOO_INPUT_SHARED);
        MQQueue outputQueue = qMgr.AccessQueue(outputQueueName, MQC.MQOO_OUTPUT);
        Console.WriteLine("Waiting get message ...");
        while (true)
        {
            try
            {
                MQMessage inputMessage = new MQMessage();
                inputQueue.Get(inputMessage, gmo);

                // Process the input message here
                string inputData = inputMessage.ReadString(inputMessage.MessageLength);
                Console.WriteLine($"Receive Message[{inputData}]");
                string outputData = "Processed: " + inputData;

                MQMessage outputMessage = new MQMessage();
                outputMessage.WriteString(outputData);
                MQPutMessageOptions pmo = new MQPutMessageOptions();
                outputQueue.Put(outputMessage, pmo);
            }
            catch (MQException ex)
            {
                if (ex.ReasonCode == MQC.MQRC_NO_MSG_AVAILABLE)
                {
                    // No message available, wait for 5 seconds before trying again
                    Console.WriteLine("No message received ...");
                    Thread.Sleep(100);
                }
                else
                {
                    // Handle other MQExceptions here
                    Console.WriteLine("MQException: " + ex.Message);
                    break;
                }
            }
        }
        inputQueue?.Close();
        outputQueue?.Close();
        qMgr.Disconnect();
        Console.ReadLine();
    }
}
