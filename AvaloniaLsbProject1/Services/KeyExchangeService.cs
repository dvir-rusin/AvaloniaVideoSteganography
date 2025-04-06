using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public static class KeyExchangeService
{
    // Port for listener role
    private const int ListenerPort = 9000;
    // Port for broadcaster role
    private const int BroadcasterPort = 9001;

    /// <summary>
    /// Listens for incoming data on the specified port and returns the received bytes.
    /// </summary>
    /// <param name="port">The port on which to listen for incoming connections.</param>
    /// <returns>A task that represents the asynchronous read operation. The task result contains the received data as a byte array.</returns>
    public static async Task<byte[]> ListenAsync(int port)
    {
        TcpListener listener = new(IPAddress.Loopback, port);//loopback 127.0.0.1
        listener.Start();

        using var client = await listener.AcceptTcpClientAsync();
        using var stream = client.GetStream();
        using var ms = new MemoryStream();

        byte[] buffer = new byte[1024];
        int read;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            ms.Write(buffer, 0, read);
            if (read < buffer.Length)
                break;
        }

        listener.Stop();
        return ms.ToArray();
    }

    /// <summary>
    /// Sends the specified data to the given port on the local host.
    /// </summary>
    /// <param name="data">The data to send as a byte array.</param>
    /// <param name="port">The port to which the data should be sent.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    public static async Task SendAsync(byte[] data, int port)
    {
        using TcpClient client = new();
        await client.ConnectAsync("127.0.0.1", port);
        using NetworkStream stream = client.GetStream();

        await stream.WriteAsync(data);
    }

    /// <summary>
    /// Listens for a public key based on the specified role.
    /// </summary>
    /// <param name="role">The role of the party ("Listener" or "Broadcaster").</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the public key bytes received.</returns>
    public static Task<byte[]> ListenForPublicKeyAsync(string role)
    {
        return role == "Listener" ? ListenAsync(ListenerPort) : ListenAsync(BroadcasterPort);
    }

    /// <summary>
    /// Sends the local public key to the corresponding remote party based on the specified role.
    /// </summary>
    /// <param name="key">The local public key to send as a byte array.</param>
    /// <param name="role">The role of the party ("Listener" or "Broadcaster").</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    public static Task SendPublicKeyAsync(byte[] key, string role)
    {
        return role == "Listener" ? SendAsync(key, BroadcasterPort) : SendAsync(key, ListenerPort);
    }
}
