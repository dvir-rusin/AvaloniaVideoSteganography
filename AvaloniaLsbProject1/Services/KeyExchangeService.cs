using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public static class KeyExchangeService
{
    private const int ListenerPort = 9000;
    private const int BroadcasterPort = 9001;

    public static async Task<byte[]> ListenAsync(int port)
    {
        TcpListener listener = new(IPAddress.Loopback, port);
        listener.Start();

        using var client = await listener.AcceptTcpClientAsync();
        using var stream = client.GetStream();
        using var ms = new MemoryStream();

        byte[] buffer = new byte[1024];
        int read;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            ms.Write(buffer, 0, read);
            if (read < buffer.Length) break;
        }

        listener.Stop();
        return ms.ToArray();
    }

    public static async Task SendAsync(byte[] data, int port)
    {
        using TcpClient client = new();
        await client.ConnectAsync("127.0.0.1", port);
        using NetworkStream stream = client.GetStream();

        await stream.WriteAsync(data);
    }

    public static Task<byte[]> ListenForPublicKeyAsync(string role)
    {
        return role == "Listener" ? ListenAsync(ListenerPort) : ListenAsync(BroadcasterPort);
    }

    public static Task SendPublicKeyAsync(byte[] key, string role)
    {
        return role == "Listener" ? SendAsync(key, BroadcasterPort) : SendAsync(key, ListenerPort);
    }
}
