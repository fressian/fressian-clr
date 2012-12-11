//   Copyright (c) ThorTech Solutions, LLC. All rights reserved.
//   The use and distribution terms for this software are covered by the
//   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
//   which can be found in the file epl-v10.html at the root of this distribution.
//   By using this software in any fashion, you are agreeing to be bound by
//   the terms of this license.
//   You must not remove this notice, or any other, from this software.
//
//   Author:  Frank Failla
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using org.fressian;

namespace fressian_server
{
    public class Program
    {
        internal static IPAddress IPADDRESS = IPAddress.Any;
        internal static int PORT = 19876;
        internal static ManualResetEvent SHUTDOWN_EVENT = new ManualResetEvent(false);

        internal static void client(IPAddress host, int port, long n)
        {
            Random rnd = new Random((int)DateTime.Now.Ticks);
            IList<object> data = Enumerable.Select(Enumerable.Range(0, (int)n), x => (object)rnd.NextDouble()).ToList();
            //IList<float[]> data = Enumerable.Select(Enumerable.Range(0, (int)n), x => new float[] {1.2F}).ToList();

            TcpClient clientSocket = new TcpClient();
            clientSocket.Connect(new IPEndPoint(host, port));
            //clientSocket.Connect(new IPEndPoint(IPAddress.Parse("10.99.0.113"), PORT));
            //clientSocket.Connect(new IPEndPoint(IPAddress.Parse("10.40.5.142"), PORT));

            using (NetworkStream stream = clientSocket.GetStream())
            {
                //write the number of objects that are going to be sent
                writeAckOrNumFressianObjects(stream, n);

                //read the ack
                long ackn = readAckOrNumOfFressianObjects(stream);
                if (n != ackn)
                    throw new System.IO.InvalidDataException(String.Format("Expected {0} for ACK, got {1}", n, ackn));

                //write the data
                writeFressianObject(stream, data);
                
                //read the data
                IList<object> ret = readFressianObjects(stream, n);
                stream.Close();
            }
        }

        internal static void writeAckOrNumFressianObjects(Stream stream, long n)
        {
            BinaryWriter bw = new BinaryWriter(stream);
            byte[] writebuf = BitConverter.GetBytes(n);
            Array.Reverse(writebuf); //clr is little endian, jvm is big endian -- this protocol will be big endian
            bw.Write(writebuf);
        }

        internal static long readAckOrNumOfFressianObjects(Stream stream)
        {
            BinaryReader br = new BinaryReader(stream);
            byte[] readbuf = br.ReadBytes(8);
            Array.Reverse(readbuf); //clr is little endian, jvm is big endian -- this protocol will be big endian
            long ackn = BitConverter.ToInt64(readbuf, 0);
            return ackn;
        }

        internal static IList<object> readFressianObjects(Stream stream, long n)
        {
            IList<object> ret = new List<object>();
            using (FressianReader rdr = new FressianReader(stream, null, true))
            {
                for (long i = 0; i < n; ++i)
                {
                    var d = rdr.readObject();
                    Console.WriteLine("[{0}]\tRead {1} from fressian socket", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"), d);
                    rdr.validateFooter();
                    Console.WriteLine("[{0}]\tValidated footer for {1} from fressian socket", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"), d);
                    ret.Add(d);
                }
            }
            return ret;
        }

        internal static void writeFressianObject<T>(Stream stream, IList<T> objects)
        {
            using (FressianWriter wtr = FressianWriter.CreateFressianWriter(stream, null))
            {
                foreach (object d in objects)
                {
                    wtr.writeObject(d);
                    Console.WriteLine("[{0}]\tWrote {1} back to fressian socket", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"), d);
                    wtr.writeFooter();
                    Console.WriteLine("[{0}]\tWrote footer for {1} back to fressian socket", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"), d);
                }
            }
        }

        internal static void server(TcpListener serverSocket)
        {
            serverSocket.Start();
            Console.WriteLine("[{0}]\tFressian Server Started", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            while (!SHUTDOWN_EVENT.WaitOne(0))
            {
                try
                {
                    TcpClient clientSocket = serverSocket.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(new WaitCallback(new Action<object>(o =>
                    {
                        //Console.WriteLine("ThreadId: {0}", Thread.CurrentThread.ManagedThreadId);
                        try
                        {
                            Console.WriteLine("[{0}]\tFressian Server Accepted Incoming Connection", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                            using (NetworkStream stream = clientSocket.GetStream())
                            {
                                //first read the number of objects to process
                                long n = readAckOrNumOfFressianObjects(stream);

                                //ack back with n
                                writeAckOrNumFressianObjects(stream, n);

                                //now read the objects
                                IList<object> objectsRead = readFressianObjects(stream, n);

                                //now write the objects
                                writeFressianObject(stream, objectsRead);
                                stream.Close();
                            }
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine(e.Message);
                        }
                        finally
                        {
                            clientSocket.Close();
                        }
                    }
                    )));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                    SHUTDOWN_EVENT.Set();
                }
            }       
        }

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Thread thread = null;
                TcpListener serverSocket = new TcpListener(new IPEndPoint(IPADDRESS, PORT));
                var svr = new Action(() =>
                {
                    thread = Thread.CurrentThread;
                    server(serverSocket);
                });

                var task = Task.Factory.StartNew(svr, TaskCreationOptions.None);

                while (Console.ReadLine() != "exit") { }
                SHUTDOWN_EVENT.Set();
                serverSocket.Stop();
                Console.WriteLine("Exiting Fressian Server...");
            }
            else  // will parse the command line args[0] and use that to send random doubles to localhost server
            {
                client(IPADDRESS, PORT, Convert.ToInt64(args[0]));
            }
        }
    }
}
