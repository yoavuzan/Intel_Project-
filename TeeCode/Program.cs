using Intel.Dal;
using System;
using System.Security.Cryptography;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace targil5Host
{
    class Program
    {

        static int otp; // send the otp to server
        static Jhi jhi = Jhi.Instance;
        static JhiSession session;
        static byte[] recvBuff = new byte[256];//Recve Buffer
        static byte[] sendBuff = new byte[260];//Send Buffer
        static void Main(string[] args)
        {
            #if AMULET
                        // When compiled for Amulet the Jhi.DisableDllValidation flag is set to true 
                        // in order to load the JHI.dll without DLL verification.
                        // This is done because the JHI.dll is not in the regular JHI installation folder, 
                        // and therefore will not be found by the JhiSharp.dll.
                        // After disabling the .dll validation, the JHI.dll will be loaded using the Windows search path
                        // and not by the JhiSharp.dll (see http://msdn.microsoft.com/en-us/library/7d83bc18(v=vs.100).aspx for 
                        // details on the search path that is used by Windows to locate a DLL) 
                        // In this case the JHI.dll will be loaded from the $(OutDir) folder (bin\Amulet by default),
                        // which is the directory where the executable module for the current process is located.
                        // The JHI.dll was placed in the bin\Amulet folder during project build.
                        Jhi.DisableDllValidation = true;
            #endif
                        // This is the UUID of this Trusted Application (TA).
                        //The UUID is the same value as the applet.id field in the Intel(R) DAL Trusted Application manifest.
                        string appletID = "1bb1651b-5389-47bf-b6e6-1d4216ce022c";
                        // This is the path to the Intel Intel(R) DAL Trusted Application .dalp file that was created by the Intel(R) DAL Eclipse plug-in.
                        string appletPath = "C:/Users/becky/eclipse-workspace\\targil5\\bin\\targil5.dalp";
                       // string appletPath = "C:/Users/becky/eclipse-workspace\\targil5\\bin\\targil5-debug.dalp";

                        // Install the Trusted Application
                        Console.WriteLine("Installing the applet.");
                        jhi.Install(appletID, appletPath);

                        // Start a session with the Trusted Application
                        byte[] initBuffer = new byte[] { }; // Data to send to the applet onInit function
                        Console.WriteLine("Opening a session.");
                        jhi.CreateSession(appletID, JHI_SESSION_FLAGS.None, initBuffer, out session);
                       
                        string card;//for input
                        int id;//for input

            //for card input
            do { 
                Console.WriteLine("please enter credit card:");
                card = Console.ReadLine();
                 }
             while (!CheckCard(card));
            
            
            // id input
            do { Console.WriteLine("please enter your 9 digit Id:"); }
            while (!(int.TryParse(Console.ReadLine(), out id) && CheckId(id)));
            
            SetToTee(UTF32Encoding.UTF8.GetBytes(card),1);
            //connect to soket pay for take a public key
             using (WebSocket ws = new WebSocket("ws://127.0.0.1:80/Pay"))
                         {
                             ws.OnMessage += ws_SendBuff;
                             ws.Connect();
                             ws.Send("SendKey");
                             Console.ReadLine();
                             ws.OnMessage -= ws_SendBuff;
                             ws.OnMessage += correctRSA;
                             ws.Send(recvBuff);//Send to Tee
                             Console.ReadLine();
                              ws.Close();
                         }
            

            using (WebSocket ws = new WebSocket("ws://127.0.0.1:80/Otp"))
            {
                ws.Connect();
                ws.Send("SendMail");
                Console.WriteLine("please check your mail and than press Enter to continue");
                Console.ReadLine();
                ws.Close();
            }

            do { Console.WriteLine("enter your otp:"); }
            while (!int.TryParse(Console.ReadLine(), out otp));

            using (WebSocket ws = new WebSocket("ws://127.0.0.1:80/Otp"))
            {
                ws.OnMessage += ws_checkOtp;
                ws.Connect();
                ws.Send(otp.ToString());
                Console.ReadLine();
            }

            // Close the session
            Console.WriteLine("Closing the session.");
            jhi.CloseSession(session);

            //Uninstall the Trusted Application
            Console.WriteLine("Uninstalling the applet.");
            jhi.Uninstall(appletID);

            Console.WriteLine("Press Enter to finish.");
            Console.Read();
      
        }
        /// <summary>
        /// check if we the program faild
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void correctRSA(object sender, MessageEventArgs e)
        {
            if (e.Data == "WorngCard")
            { 
                Console.WriteLine("sorry your card is not valied");
                Environment.Exit(0);
            }
            
        }
    
        /// <summary>
        /// print a message to client for correct or worng otp  
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ws_checkOtp(object sender, MessageEventArgs e)
        {
            if (e.Data == "correct")  
                Console.WriteLine("correct otp start to fill gas"); 
            else
             Console.WriteLine("worng otp ! soryy "); 
        }

        /// <summary>
        /// send the public key to Tee
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ws_SendBuff(object sender, MessageEventArgs e)
        {
            SetToTee(e.RawData,2);
        }

        /// <summary>
        ///check for correct input card 
        /// </summary>
        /// <param name="card"></param>
        /// <returns></returns>
        private static bool CheckCard(string card)
        {
            foreach (char c in card)
            {
                if (c < '0' || c > '9')
                    return false;
             }
            int len = card.Length;
            return len >= 8 && len <= 19;
        }

        /// <summary>
        ///check for correct input length Id 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private static bool CheckId(int id)
        {
            return id.ToString().Length == 9;
        }
        /// <summary>
        /// set to Tee the buffer and the order to do
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="cmdId"></param>
        private static void SetToTee(byte[] buffer,int cmdId)
        {
            Array.Copy(buffer, 0, sendBuff, 0, buffer.Length);
            int responseCode; // The return value that the TA provides using the IntelApplet.setResponseCode method
            Console.WriteLine("Performing send and receive operation.");
            jhi.SendAndRecv2(session, cmdId, sendBuff, ref recvBuff, out responseCode);
            if (cmdId == 1)
                Console.Out.WriteLine("Response buffer is: \n" + UTF32Encoding.UTF8.GetString(recvBuff) + " \n And save in Tee");
            else
            {
                Console.Out.WriteLine("Response buffer is:\n " + UTF32Encoding.UTF8.GetString(recvBuff) + " \n The encrype of card");
                Console.WriteLine("plese press enter to continue");
            }
            }
    }

}




