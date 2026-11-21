using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Kinect;

class Program
{
    static void Main()
    {
        var sensor = KinectSensor.KinectSensors
            .FirstOrDefault(s => s.Status == KinectStatus.Connected);
        if (sensor == null)
        {
            Console.WriteLine("No se encontro el Kinect");
            Console.ReadLine();
            return;
        }

        var suavizado = new TransformSmoothParameters
        {
            Smoothing = 0.5f,
            Correction = 0.5f,
            Prediction = 0.5f,
            JitterRadius = 0.05f,
            MaxDeviationRadius = 0.04f
        };
        sensor.SkeletonStream.Enable(suavizado);

        var skeletons = new Skeleton[sensor.SkeletonStream.FrameSkeletonArrayLength];
        var udp = new UdpClient();
        var destino = new IPEndPoint(IPAddress.Loopback, 5005);

        sensor.SkeletonFrameReady += (s, e) =>
        {
            using (var frame = e.OpenSkeletonFrame())
            {
                if (frame == null) return;
                frame.CopySkeletonDataTo(skeletons);

                var sk = skeletons.FirstOrDefault(x => x.TrackingState == SkeletonTrackingState.Tracked);
                string msg;
                if (sk == null) msg = "NONE";
                else
                {
                    var sb = new StringBuilder();
                    foreach (Joint j in sk.Joints)
                        sb.AppendFormat(CultureInfo.InvariantCulture, "{0}:{1:F3},{2:F3},{3:F3};",
                            j.JointType, j.Position.X, j.Position.Y, j.Position.Z);
                    msg = sb.ToString();
                }
                var bytes = Encoding.ASCII.GetBytes(msg);
                udp.Send(bytes, bytes.Length, destino);
            }
        };

        sensor.Start();
        Console.WriteLine("Kinect enviando a UDP 5005. Enter para salir.");
        Console.ReadLine();
        sensor.Stop();
    }
}