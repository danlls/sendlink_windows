using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

public class SocketHandler
{
    private static Socket socket;

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static Socket getSocket()
    {
        return socket;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void setSocket(Socket socket)
    {
        SocketHandler.socket = socket;
    }
}