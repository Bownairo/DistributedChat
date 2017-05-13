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
using Newtonsoft.Json;

public class ComObject
{
    public bool New;
    public string Address;
    public string Websocket;
}

public class TCPHandler
{
    static List<string> ServerList;
	static byte[] key;
	static byte[] iv;
	public const int BufferSize = 4096;

	public TCPHandler() {
		key = new byte[] { 136, 77, 169, 60, 5, 109, 61, 3, 23, 226, 114, 139, 240, 73, 52, 234 };
	 	iv = new byte[] { 178, 102, 153, 177, 84, 41, 185, 203, 15, 20, 139, 186, 170, 114, 181, 13 };
	}


	private byte[] Encrypt(string plainText)
	{
		// Check arguments.
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

    public async void StartComServer()
    {
        var listener = new TcpListener(IPAddress.Any, 6000);
		var buffer = new byte[BufferSize];
        listener.Start();
        for(;;) {
            var client = await listener.AcceptTcpClientAsync();
            var sr = client.GetStream();
            var sw = client.GetStream();
			sr.Read(buffer, 0, BufferSize);
            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            var data = JsonConvert.DeserializeObject<ComObject>(Decrypt(buffer), settings);

            if (data.New)
            {
                if(ServerList == null)
                    ServerList = new List<string>();
                ProxyModel.Instance.AddServer(data.Websocket);

                var list = JsonConvert.SerializeObject(ServerList);
				var b_list = Encrypt(list);
				sw.Write(b_list, 0, b_list.Length);

                ServerList.Add(data.Address);
                sw.Flush();
            }
            else
            	ProxyModel.Instance.UserLeftServer(data.Websocket);

            sw.Dispose();
            sr.Dispose();
            client.Dispose();
        }
    }
}
