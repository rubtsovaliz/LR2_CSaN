using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LR2_CSaN
{
    class Program
    {
        const int TYPE_ECHO_REQUEST = 8;
        const int MESSAGE_MAX_SIZE = 1024;

        static void Main(string[] args)
        {
            byte[] message = new byte[MESSAGE_MAX_SIZE];
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            Console.Write("Введите IP-адрес для трассировки: ");
            string ipAddress = Console.ReadLine(); 

            try
            {
                IPEndPoint destIPEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), 0);
                message = Encoding.ASCII.GetBytes("ICMP packet!");
                ICMPPacket packet = new ICMPPacket(TYPE_ECHO_REQUEST, message);

                Traceroute(socket, packet, destIPEndPoint);
            }
            catch (FormatException)
            {
                Console.WriteLine("Недопустимый IP-адрес: {0}.", ipAddress);
            }
            catch (SocketException)
            {
                Console.WriteLine("Не удается связаться с узлом по адресу {0}.", ipAddress);
            }
            socket.Close();
        }

        static void Traceroute(Socket socket, ICMPPacket packet, IPEndPoint destIPEndPoint)
        {
            int timeStart, timeEnd, responseSize, errCount = 0;
            byte[] responseMessage;

            EndPoint hopEndPoint = destIPEndPoint;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 3000);
            for (int i = 1; i < 50; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, i);
                    timeStart = Environment.TickCount;
                    socket.SendTo(packet.getBytes(), packet.PacketSize, SocketFlags.None, destIPEndPoint);
                    try
                    {
                        responseMessage = new byte[MESSAGE_MAX_SIZE];
                        responseSize = socket.ReceiveFrom(responseMessage, ref hopEndPoint);
                        timeEnd = Environment.TickCount;
                        ICMPPacket response = new ICMPPacket(responseMessage, responseSize);

                        Console.Write("{0} мс\t", timeEnd - timeStart);
                        if (j == 2)
                        {
                            Console.WriteLine("{0}", hopEndPoint.ToString());
                        }

                        if ((response.Type == 0) && (j == 2))
                        {
                            Console.WriteLine("Трассировка завершена.");
                            return;
                        }
                        errCount = 0;
                    }
                    catch (SocketException)
                    {
                        Console.Write("*\t");
                        if (j == 2)
                        {
                            Console.WriteLine("{0}: Превышен интервал ожидания для запроса.", i);
                        }
                        errCount++;
                        if (errCount == 30)
                        {
                            Console.WriteLine("Невозможно связаться с удаленным хостом.");
                            return;
                        }
                    }
                }
            }
        }
    }
}