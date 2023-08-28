using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Security.Cryptography;
using FluentEmail.Smtp;
using System.Net.Mail;
using System.Threading.Tasks;
using FluentEmail.Core;
using System.IO;
using System.Xml.Serialization;


namespace Server1
{

    /// <summary>
    /// class thad handles with otp 
    /// </summary>
    public class Otp : WebSocketBehavior
    {
        static string otpStr = null; //parm for random otp 
        /// <summary>
        ///fuction for all message from client 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMessage(MessageEventArgs e)
        {
            if (e.Data== "SendMail") // send otp to client mail
            {
                Random rand = new Random();
                int otp = rand.Next(100000, 999999); //get otp with 6 digit
                otpStr = otp.ToString(); //convert to string
                SendEmail(otpStr);
            }
            if(e.Data.Length < 7 && otpStr!=null) 
            { 
            if(e.Data == otpStr) //check if the cliant Enter a correct otp
                    Send("correct");
                else
                    Send("worng");
            }
        }


        /// <summary>
        /// Function that get a otp messge for send to client 
        /// </summary>
        /// <param name="message"></param>
        public void SendEmail(string message)
        {
            var sender = new SmtpSender(() => new SmtpClient("localhost")
            {
                EnableSsl = false,
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = @"c:\Demos"
            });

            Email.DefaultSender = sender;
            var email = Email
            .From("OTPserver@gmail.com")
            .To("test@hi.com", "Hello from ")
            .Subject("Thanks ")
            .Body($"your otp is {message}")
            .Send();
        }
    }
    /// <summary>
    /// class thad handles with client pay 
    /// </summary>
    public class PayCredyCard : WebSocketBehavior
    {

        static RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);//start rsaAlgo
        static RSAParameters param = new RSAParameters();//paramter for create public key
        /// <summary>
        /// fuction for all message from client 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMessage(MessageEventArgs e)
        {

            param = rsa.ExportParameters(true);//
            if (e.Data == "SendKey")// send a public key to TEE 
            {
                byte[] ex= new byte[4];
                byte[] n = new byte[256];
                n = param.Modulus;
                byte[] publickey = new byte[260];
                Array.Copy(param.Exponent,0,ex,1,param.Exponent.Length);
                Array.Copy(n, 0, publickey, 0, 256);
                Array.Copy(ex, 0, publickey, 256, 4);
                Send(publickey);
            }

            if (e.RawData.Length > 7)//decrypt the card 
            {
                try {
                    byte[] decryptData = rsa.Decrypt(e.RawData, false);
                Console.WriteLine(UTF32Encoding.UTF8.GetString(decryptData));
                    Send("Sucsses");//Sucsses decryption
                }
                catch(Exception)
                { Send("WorngCard"); }

            }

        }
    }

    public class Server
    {
        public static int Main(String[] args)
        {
            WebSocketServer server = new WebSocketServer("ws://127.0.0.1:80"); //create the server
            server.AddWebSocketService<PayCredyCard>("/Pay");// add socket "pay"
            server.AddWebSocketService<Otp>("/Otp");// add socket "otp"
            server.Start(); //open the server
            //Send key
            Console.WriteLine("connect");
            Console.ReadLine();
            return 0;
        }
    }
}