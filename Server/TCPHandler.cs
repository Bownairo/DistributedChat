using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

using WebApplication;

public class ComObject
{
    public bool New;
    public string Address;
    public string Websocket;
}

public class InternalComObject
{
    public bool Add;
    public string Body;
}

public class TCPHandler
{
    static string myAddress;
    static string myWebsocket;
    static List<string> others;
	static byte[] key;
	static byte[] iv;

    public void Init(string websocket, string address)
    {
        myWebsocket = websocket;
        myAddress = address;
        others = new List<string>();
		key = new byte[] { 136, 77, 169, 60, 5, 109, 61, 3, 23, 226, 114, 139, 240, 73, 52, 234 };
	 	iv = new byte[] { 178, 102, 153, 177, 84, 41, 185, 203, 15, 20, 139, 186, 170, 114, 181, 13 };
    }

	public void PrintByteArray(byte[] bytes)
	{
	    var sb = new StringBuilder("new byte[] { ");
	    foreach (var b in bytes)
	    {
	        sb.Append(b + ", ");
	    }
	    sb.Append("}");
	    Console.WriteLine(sb.ToString());
	}

	private byte[] Encrypt(string plainText)
	{
		if (plainText == null || plainText.Length <= 0)
		throw new ArgumentNullException("plainText");
		byte[] encrypted;

		using (Aes sec = Aes.Create())
		{
			sec.Key = key;
			sec.IV = iv;
			sec.Padding = PaddingMode.Zeros;

			ICryptoTransform encryptor = sec.CreateEncryptor(sec.Key, sec.IV);

			using (MemoryStream msEncrypt = new MemoryStream())
			{
				using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
				{
					using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
					{
						swEncrypt.Write(plainText);
					}
					encrypted = msEncrypt.ToArray();
				}
			}
		}
		return encrypted;
	}

	private string Decrypt(byte[] cipherText)
	{
		if (cipherText == null || cipherText.Length <= 0)
			throw new ArgumentNullException("cipherText");
		string plaintext = null;

		using (Aes sec = Aes.Create())
		{
			sec.Key = key;
			sec.IV = iv;
			sec.Padding = PaddingMode.Zeros;

			ICryptoTransform decryptor = sec.CreateDecryptor(sec.Key, sec.IV);

			using (MemoryStream msDecrypt = new MemoryStream(cipherText))
			{
				using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
				{
					using (StreamReader srDecrypt = new StreamReader(csDecrypt))
					{
						plaintext = srDecrypt.ReadToEnd();
					}
				}
			}

		}
		var i = plaintext.IndexOf('\0');
		return plaintext.Substring(0, i);
    }

    public const int BufferSize = 4096;
    public async void StartCom()
    {
        var package = new ComObject();
        package.Address = myAddress;
        package.Websocket = myWebsocket;
        package.New = true;
		var buffer = new byte[BufferSize];

		var data = JsonConvert.SerializeObject(package);

        var client = new TcpClient();
        try
        {
            await client.ConnectAsync("localhost", 6000); //Proxy

            var sw = client.GetStream();
            var sr = client.GetStream();

			var b_data = Encrypt(data);
			sw.Write(b_data, 0, b_data.Length);
            sw.Flush();

			sr.Read(buffer, 0, BufferSize);
            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            var connectTo = JsonConvert.DeserializeObject<List<string>>(Decrypt(buffer), settings);

            foreach(var s in connectTo)
            {
                var add = new TcpClient();
                await add.ConnectAsync(s.Split(':')[0], int.Parse(s.Split(':')[1])); //Other server

				var temp = add.GetStream();

				var InternalPackage = new InternalComObject();
				InternalPackage.Add = true;
				InternalPackage.Body = myAddress;

				var b = Encrypt(JsonConvert.SerializeObject(InternalPackage, settings));
				temp.Write(b, 0, b.Length);
				temp.Flush();
                others.Add(s);
            }

            sr.Dispose();
            sw.Dispose();
            client.Dispose();
            ListenForOthers();
        }
        catch
        {
            Console.WriteLine("Can't see server");
            Environment.Exit(-1);
        }
    }

    public async void ListenForOthers()
    {
        Console.WriteLine("Listening for others");
        var listener = new TcpListener(IPAddress.Any, int.Parse(myAddress.Split(':')[1]));
		var buffer = new byte[BufferSize];
        listener.Start();
        for(;;) {
            var client = await listener.AcceptTcpClientAsync();
            //add a check to make sure it's not a closing doober or maybe not
			var sr = client.GetStream();
			//var sr = new StreamReader(client.GetStream());
			sr.Read(buffer, 0, BufferSize);

			var message = JsonConvert.DeserializeObject<InternalComObject>(Decrypt(buffer));
            if(message.Add)
            {
                Console.WriteLine("Added server");
                others.Add(message.Body);
            }
            else
            {
                await RelayModel.Instance.PropogateMessage(message.Body);
            }

            sr.Dispose();
        }
    }

    public async void Relay(string message)
    {
        var package = new InternalComObject();
        package.Add = false;
        package.Body = message;

		var buffer = new byte[BufferSize];
        var data = JsonConvert.SerializeObject(package);
		byte[] encrypted = new byte[BufferSize];

        foreach(var other in others)
        {
            var client = new TcpClient();
            try
            {
                await client.ConnectAsync(other.Split(':')[0], int.Parse(other.Split(':')[1]));
				var sw =  client.GetStream();

				var b = Encrypt(data);
				sw.Write(b, 0, b.Length);
                sw.Flush();
                sw.Dispose();
                client.Dispose();
            }
            catch
            {
                Console.WriteLine("Can't connect to other servers");
            }
        }
    }

    public async void UserLeft()
    {
        var package = new ComObject();
        package.Address = myAddress;
        package.Websocket = myWebsocket;
        package.New = false;

        var data = JsonConvert.SerializeObject(package);

        var client = new TcpClient();
        try
        {
            await client.ConnectAsync("localhost", 5001); //Proxy

            var sw = new StreamWriter(client.GetStream());
            sw.Write(data);
            sw.Flush();
            sw.Dispose();
            client.Dispose();
        }
        catch
        {
            Console.WriteLine("Can't see server");
            Environment.Exit(-1);
        }
    }
}
