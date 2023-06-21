#region Assembly PhoneFarmLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null

#endregion

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using AE.Net.Mail;
using Newtonsoft.Json.Linq;

namespace PhoneFarmLib
{
    public class ImapHelper
    {
        public static string ConnectImap(int type, string username, string password, int timeout = 120, string imap = "")
        {
            //IL_007f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0085: Expected O, but got Unknown
            //IL_00cb: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d1: Expected O, but got Unknown
            //IL_00f0: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f6: Expected O, but got Unknown
            string text = "";
            int num = 0;
            while (true)
            {
                int tickCount = Environment.TickCount;
                try
                {
                    ImapClient val = null;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                    do
                    {
                        int num2 = 0;
                        if (username.Split('@')[1].StartsWith("hotmail.") || username.Split('@')[1].StartsWith("outlook."))
                        {
                            num2 = 1;
                            val = new ImapClient("outlook.office365.com", username, password, (AuthMethods)0, 993, true, false);
                            val.SelectMailbox("Inbox");
                        }
                        else
                        {
                            num2 = 0;
                            if (username.Split('@')[1].Contains("gmail.com"))
                            {
                                val = new ImapClient("imap.gmail.com", username, password, (AuthMethods)0, 993, true, false);
                                val.SelectMailbox("Inbox");
                            }
                            else
                            {
                                val = new ImapClient("imap.yandex.com", username, password, (AuthMethods)0, 993, true, false);
                                val.SelectMailbox("Spam");
                            }
                        }

                        try
                        {
                            Lazy<MailMessage>[] array = null;
                            if (num2 == 0 || type == 3)
                            {
                                array = val.SearchMessages(SearchCondition.To(username), false, false);
                            }
                            else
                            {
                                array = val.SearchMessages(SearchCondition.From("security@facebookmail.com").And((SearchCondition[])(object)new SearchCondition[1] { SearchCondition.Unseen() }), false, false);
                                if (array == null || array.Length == 0)
                                {
                                    array = val.SearchMessages(SearchCondition.From("registration@facebookmail.com").And((SearchCondition[])(object)new SearchCondition[1] { SearchCondition.Unseen() }), false, false);
                                }
                            }

                            if (array != null && array.Length != 0)
                            {
                                int num3 = array.Count() - 1;
                                while (num3 >= 0)
                                {
                                    MailMessage value = array[num3].Value;
                                    string oldstring = ((ObjectWHeaders)((Collection<Attachment>)(object)value.get_AlternateViews())[1]).get_Body().ToString();
                                    text = ExtractCode(oldstring, type, imap);
                                    val.DeleteMessage(value);
                                    if (!(text != ""))
                                    {
                                        num3--;
                                        continue;
                                    }

                                    if (!((TextClient)val).get_IsDisposed())
                                    {
                                        ((TextClient)val).Dispose();
                                    }

                                    if (((TextClient)val).get_IsConnected())
                                    {
                                        ((TextClient)val).Disconnect();
                                        return text;
                                    }

                                    return text;
                                }
                            }
                        }
                        catch (Exception)
                        {
                        }

                        if (!((TextClient)val).get_IsDisposed())
                        {
                            ((TextClient)val).Dispose();
                        }

                        if (((TextClient)val).get_IsConnected())
                        {
                            ((TextClient)val).Disconnect();
                        }
                    }
                    while (Environment.TickCount - tickCount < timeout * 1000);
                }
                catch (Exception ex2)
                {
                    if (ex2.ToString().Contains("The remote certificate is invalid according to the validation procedure") || ex2.ToString().Contains("An established connection was aborted by the software in your host machine"))
                    {
                        num++;
                        if (num < 3)
                        {
                            continue;
                        }
                    }
                    else if (ex2.ToString().ToLower().Contains("blocked"))
                    {
                        return "block";
                    }

                    return "not connect";
                }

                break;
            }

            return text;
        }

        public static string ExtractCode(string oldstring, int type, string additionalInfo = "")
        {
            string text = "";
            switch (type)
            {
                case 0:
                    text = Regex.Match(oldstring, "https://www.facebook.com/confirmcontact.php(.*?)\"").Value.Trim().Replace("&amp;", "&").Replace("\"", "");
                    if (text == "")
                    {
                        text = Regex.Match(oldstring, "https://www.facebook.com/confirmcontact.php(.*?)\n").Value.Trim().Replace("&amp;", "&").Replace("\"", "");
                    }

                    if (text == "")
                    {
                        text = Regex.Match(oldstring, "https://www.facebook.com/n/\\?confirmemail.php(.*?)\n").Value.Trim().Replace("&amp;", "&").Replace("\"", "");
                    }

                    break;
                case 1:
                    text = Regex.Match(oldstring, "\\d{8}").Value.Trim();
                    if (additionalInfo.StartsWith(text))
                    {
                        text = "";
                    }

                    break;
                case 3:
                    text = Regex.Match(oldstring, ">\\d+<").Value.Trim().Replace(">", "").Replace("<", "");
                    if (text == "")
                    {
                        text = Regex.Match(oldstring, "Security code: \\d+").Value;
                        text = Regex.Match(text, "\\d+").Value;
                    }

                    break;
                case 2:
                    text = Regex.Match(oldstring, "c=(\\d+)&").Groups[1].Value;
                    break;
                case 4:
                    {
                        string value = Regex.Match(oldstring, "\\?n=(.*?)&").Groups[1].Value;
                        string value2 = Regex.Match(oldstring, ";id=(.*?)&").Groups[1].Value;
                        if (value != "" && value2 != "")
                        {
                            text = "https://m.facebook.com/recover/password/?u=" + value2 + "&n=" + value + "&fl=default_recover&sih=0&msgr=0";
                        }

                        break;
                    }
            }

            return text;
        }

        internal static string ExtractConfirmationCode(int type, string url, string token, string additionalInfo = "", int timeoutSeconds = 60)
        {
            RequestXNet requestXNet = new RequestXNet("", "", "", 0);
            url = url.Substring(0, url.LastIndexOf("=") + 1) + token;
            int tickCount = Environment.TickCount;
            do
            {
                string responseFromUrl = requestXNet.GetResponseFromUrl(url);
                try
                {
                    responseFromUrl = "{\"data\":" + responseFromUrl + "}";
                    JObject jObject = JObject.Parse(responseFromUrl);
                    for (int num = jObject["data"].Count() - 1; num >= 0; num--)
                    {
                        jObject["data"]![num]!["created_at"]!.ToString();
                        string oldstring = jObject["data"]![num]!["body"]!.ToString();
                        string text = ExtractCode(oldstring, type, additionalInfo);
                        if (text != "")
                        {
                            text = Regex.Match(text, "c=(\\d+)&").Value;
                            if (text != "")
                            {
                                return text;
                            }
                        }
                    }
                }
                catch
                {
                }

                Common.Delay(3.0);
            }
            while (Environment.TickCount - tickCount < timeoutSeconds * 1000);
            return "";
        }

        public static bool CheckConnectImap(string username, string password)
        {
            //IL_0081: Unknown result type (might be due to invalid IL or missing references)
            //IL_0087: Expected O, but got Unknown
            int num = 0;
            while (true)
            {
                try
                {
                    string text = "";
                    if (username.EndsWith("@hotmail.com") || username.EndsWith("@outlook.com") || username.EndsWith("@nickpromail.com"))
                    {
                        text = "outlook.office365.com";
                    }
                    else if (username.EndsWith("@yandex.com"))
                    {
                        text = "imap.yandex.com";
                    }

                    if (text == "")
                    {
                        return false;
                    }

                    ImapClient val = new ImapClient(text, username, password, (AuthMethods)0, 993, true, false);
                    ((TextClient)val).Dispose();
                    return true;
                }
                catch (Exception ex)
                {
                    if (!ex.ToString().Contains("The remote certificate is invalid according to the validation procedure"))
                    {
                        goto IL_00ca;
                    }

                    num++;
                    if (num >= 10)
                    {
                        goto IL_00ca;
                    }

                    goto end_IL_0093;
                IL_00ca:
                    return false;
                end_IL_0093:;
                }
            }
        }

        internal static string ExtractVerificationCode(int type, string url, string token, int timeoutSeconds = 60)
        {
            RequestXNet requestXNet = new RequestXNet("", "", "", 0);
            url = url.Substring(0, url.LastIndexOf("=") + 1) + token;
            int tickCount = Environment.TickCount;
            do
            {
                string responseFromUrl = requestXNet.GetResponseFromUrl(url);
                try
                {
                    responseFromUrl = "{\"data\":" + responseFromUrl + "}";
                    JObject jObject = JObject.Parse(responseFromUrl);
                    int num = jObject["data"].Count() - 1;
                    while (num >= 0)
                    {
                        jObject["data"]![num]!["created_at"]!.ToString();
                        string input = jObject["data"]![num]!["body"]!.ToString();
                        string value = Regex.Match(input, ">(\\d+)<").Groups[1].Value;
                        if (!(value != ""))
                        {
                            num--;
                            continue;
                        }

                        return value;
                    }
                }
                catch
                {
                }

                Common.Delay(3.0);
            }
            while (Environment.TickCount - tickCount < timeoutSeconds * 1000);
            return "";
        }
    }
}
#if false // Decompilation log
'28' items in cache
------------------
Resolve: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\mscorlib.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=12.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Found single assembly: 'Newtonsoft.Json, Version=12.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Load from: 'C:\Users\SAM2\Desktop\Nguyen maxfarm\MaxPhoneFarm_v23.04.15\MaxPhoneFarm_v23.04.15\bin\Debug\net48\Newtonsoft.Json.dll'
------------------
Resolve: 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.dll'
------------------
Resolve: 'System.Management, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Management, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'AE.Net.Mail, Version=1.7.10.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'AE.Net.Mail, Version=1.7.10.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'Http, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'Http, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\SAM2\Desktop\Nguyen maxfarm\MaxPhoneFarm_v23.04.15\MaxPhoneFarm_v23.04.15\bin\Debug\net48\Http.dll'
------------------
Resolve: 'xNet, Version=3.3.3.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'xNet, Version=3.3.3.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\SAM2\Desktop\Nguyen maxfarm\MaxPhoneFarm_v23.04.15\MaxPhoneFarm_v23.04.15\bin\Debug\net48\xNet.dll'
------------------
Resolve: 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.Core.dll'
#endif
