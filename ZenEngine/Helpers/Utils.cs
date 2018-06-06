/*************************************************************************
 * Copyright (c) 2015, 2018 Zenodys BV
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 * Contributors:
 *    Tomaž Vinko
 *   
 **************************************************************************/

using System;
using System.Collections;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text;
using ZenCommon;

namespace ComputingEngine
{
    internal class Utils
    {
        #region Fields
        #region m_LoadedAssemblies
        static Hashtable m_LoadedAssemblies = new Hashtable();
        #endregion

        #region _loadImplementationAssemblySync
        static object _loadImplementationAssemblySync = new object();
        #endregion
        #endregion

        #region AssemblyLoad Helpers
        static Assembly GetOrLoadAssembly(string sFileName)
        {
            lock (_loadImplementationAssemblySync)
            {
                AssemblyName updatedAssemblyName = AssemblyName.GetAssemblyName(sFileName);
                byte[] publicKeyToken = Assembly.GetExecutingAssembly().GetName().GetPublicKey();
                if (m_LoadedAssemblies.Contains(sFileName + "_" + updatedAssemblyName.Version))
                    return m_LoadedAssemblies[sFileName + "_" + updatedAssemblyName.Version] as Assembly;

                Assembly asm = Assembly.Load(File.ReadAllBytes(sFileName));
                if (ConfigurationManager.AppSettings["AllowUnsignedAssemblies"] == "0")
                {
                    byte[] asmA = asm.GetName().GetPublicKey();
                    if (asmA.Length == 0)
                    {
                        Console.WriteLine("Unsigned assemblies are not allowed. Set 'AllowUnsignedAssemblies' global setting to '1' or sign assembly " + sFileName);
                        Console.ReadLine();
                        return null;
                    }
                    else
                    {
                        for (int i = 0; i < publicKeyToken.Length; i++)
                        {
                            if (publicKeyToken[i] != asmA[i])
                                return null;
                        }
                    }
                }
                m_LoadedAssemblies.Add(sFileName + "_" + updatedAssemblyName.Version, asm);
                return asm;
            }
        }

        #region LoadImplementation
        public static IZenImplementation LoadImplementation(string filename)
        {
            try
            {
                Assembly asm = GetOrLoadAssembly(filename);
                if (asm == null)
                    return null;

                Type[] t = asm.GetTypes();
                for (int i = 0; i < t.Length; i++)
                {
                    ConstructorInfo myConstructor = t[i].GetConstructor(new Type[0]);
                    try
                    {
                        if (myConstructor != null)
                        {
                            var myObject = myConstructor.Invoke(new object[0]);
                            if (myObject is IZenImplementation)
                                return (IZenImplementation)myObject;
                        }
                    }
                    catch { }
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine("Critical exception in loading " + filename + " : " + (ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.Message.Trim()) ? ex.InnerException.Message : ex.Message));
                Console.ReadLine();
            }

            return null;
        }
        #endregion
        #endregion

        internal static string Decrypt(string filePath, string password)
        {
            string decryptedStr = string.Empty;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                MemoryStream ms = new MemoryStream();
                SharpAESCrypt.SharpAESCrypt.Decrypt(password, fs, ms);
                ms.Position = 0;
                var sr = new StreamReader(ms);
                decryptedStr = sr.ReadToEnd();
                fs.Close();
            }
            return decryptedStr;
        }

        internal static long ToJavascriptTimestamp(DateTime input)
        {
            if (input == DateTime.MinValue)
                return 0;

            TimeSpan span = new TimeSpan(new DateTime(1970, 1, 1, 0, 0, 0).Ticks);
            DateTime time = input.Subtract(span);
            return (long)(time.Ticks / 10000);
        }

        internal static byte[] GetPrivateKeyByteArray(string key)
        {
            string[] aKey = key.Split(' ');
            byte[] bKey = new byte[16];

            for (int i = 0; i < aKey.Length; i++)
            {
                if (aKey[i].Trim() != string.Empty)
                    bKey[i] = Convert.ToByte(aKey[i]);
            }
            return bKey;
        }

        public static byte[] DecryptRaw(byte[] encryptedString, byte[] key)
        {
            byte[] decrypted_bytes = new byte[encryptedString.Length];
            Xtea.Decrypt(key, encryptedString, decrypted_bytes, encryptedString.Length);
            return decrypted_bytes;


        }
    }

    public static class Xtea
    {

        public static Boolean Decrypt(byte[] key, byte[] encryptedData, byte[] plainData, int dataSize)
        {
            if ((dataSize % 8) == 0)
            {
                if (dataSize != 0)
                {
                    dataSize /= 8;
                    UInt32 i;
                    UInt32[] v = new UInt32[2];
                    UInt32 sum;
                    UInt32[] lkey = new UInt32[4];
                    int eoff = 0;
                    int poff = 0;

                    Buffer.BlockCopy(key, 0, lkey, 0, 16);
                    const UInt32 delta = 0x9E3779B9;
                    while (dataSize-- != 0)
                    {
                        Buffer.BlockCopy(encryptedData, eoff, v, 0, 8);
                        sum = 0xC6EF3720;

                        for (i = 0; i < 32; i++)
                        {
                            v[1] -= (v[0] << 4 ^ v[0] >> 5) + v[0] ^ sum + lkey[sum >> 11 & 3];
                            sum -= delta;
                            v[0] -= (v[1] << 4 ^ v[1] >> 5) + v[1] ^ sum + lkey[sum & 3];
                        }
                        Buffer.BlockCopy(v, 0, plainData, poff, 8);
                        poff += 8;
                        eoff += 8;
                    }
                }
                return true;
            }
            return false;
        }
        public static Boolean Encrypt(byte[] key, byte[] plainData, byte[] encryptedData, int dataSize)
        {
            if (((dataSize % 8) == 0))
            {
                if (dataSize != 0)
                {
                    dataSize /= 8;
                    UInt32 i;
                    UInt32[] v = new UInt32[2];
                    UInt32 sum;
                    UInt32[] lkey = new UInt32[4];
                    int eoff = 0;
                    int poff = 0;
                    Buffer.BlockCopy(key, 0, lkey, 0, 16);

                    const UInt32 delta = 0x9E3779B9;
                    while (dataSize-- != 0)
                    {
                        Buffer.BlockCopy(plainData, poff, v, 0, 8);
                        sum = 0;

                        for (i = 0; i < 32; i++)
                        {
                            v[0] += (((v[1] << 4) ^ (v[1] >> 5)) + v[1]) ^ (sum + lkey[sum & 3]);
                            sum += delta;
                            v[1] += (((v[0] << 4) ^ (v[0] >> 5)) + v[0]) ^ (sum + lkey[(sum >> 11) & 3]);
                        }
                        Buffer.BlockCopy(v, 0, encryptedData, eoff, 8);
                        poff += 8;
                        eoff += 8;
                    }
                }

                return true;
            }

            return false;
        }
    }
}
