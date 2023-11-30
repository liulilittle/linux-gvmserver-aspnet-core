namespace GVMServer.W3Xiyou.Docking.Api
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Threading;

    public class XiYouDot
    {
        private class DotDotRequest
        {
            public string cat { get; set; }

            public string act { get; set; }

            public string ic { get; set; }

            public string apid { get; set; }

            public string paid { get; set; }

            public long ts { get; set; }

            public int event_code { get; set; }

            public string event_des { get; set; }

            public int event_show { get; set; }

            public string imeio { get; set; }

            public string idfao { get; set; }
        }

        public static void DotAsync(
            string appid,
            string paid,
            int eventCode,
            string eventDescription,
            string imeio,
            string idfao,
            Action<GenericResponse<object>> callback,
            int retransmission)
        {
            InternalDot(appid, paid, eventCode, eventDescription, imeio, idfao, callback, retransmission, true);
        }

        public static GenericResponse<object> Dot(
            string appid,
            string paid,
            int eventCode,
            string eventDescription,
            string imeio,
            string idfao)
        {
            GenericResponse<object> response = null;
            InternalDot(appid, paid, eventCode, eventDescription, imeio, idfao, (e) => response = e, 0, false);
            return response;
        }

        private static void InternalDot(
            string appid,
            string paid,
            int eventCode,
            string eventDescription,
            string imeio,
            string idfao,
            Action<GenericResponse<object>> callback,
            int retransmission,
            bool async)
        {
            if (string.IsNullOrEmpty(appid) ||
                string.IsNullOrEmpty(eventDescription) ||
                string.IsNullOrEmpty(imeio) ||
                string.IsNullOrEmpty(idfao))
            {
                callback?.Invoke(new GenericResponse<object>() { Code = (int)XiYouSdkNonError.XiYouSdkNonError_kInvalidParameter });
                return;
            }

            DotDotRequest request = new DotDotRequest();
            request.cat = "App";
            request.act = "Monitor";
            request.ic = "TrackEvent";
            request.event_show = 1;
            request.ts = XiYouUtility.ToTimeSpan13(DateTime.Now);

            request.apid = appid;
            request.imeio = imeio;
            request.idfao = idfao;
            request.paid = paid ?? string.Empty;

            request.event_code = eventCode;
            request.event_des = eventDescription;

            string message = XiYouSerializer.FetchSignTextOrMessage(request);
            if (async)
            {
                Action executeRequestAsync = null;
                XiYouUtility.PostAsyncCallback<GenericResponse<object>> postAsyncCallback = null;
                postAsyncCallback = (error, e) =>
                {
                    if (error != XiYouUtility.PostAsyncError.kTimeout)
                    {
                        postAsyncCallback = null;
                        executeRequestAsync = null;
                    }
                    else
                    {
                        if (Interlocked.Decrement(ref retransmission) < 0)
                        {
                            postAsyncCallback = null;
                            executeRequestAsync = null;
                            e = new GenericResponse<object>();
                            e.Code = (int)XiYouSdkNonError.XiYouSdkNonError_kTimeout;
                        }
                        else
                        {
                            executeRequestAsync();
                        }
                    }
                    callback?.Invoke(e);
                };
                executeRequestAsync = () => XiYouUtility.PostToUrlAsync("https://analytics.52xiyou.com/mo.json", message, postAsyncCallback, XiYouUtility.DefaultTimeout);
                executeRequestAsync();
            }
            else
            {
                var response = XiYouUtility.PostToUrl<GenericResponse<object>>("https://analytics.52xiyou.com/mo.json", message,
                    XiYouUtility.DefaultTimeout);
                if (response == null)
                {
                    response = new GenericResponse<object>() { Code = -1 };
                }
                callback(response);
            }
        }

        /// <summary>
        /// 打点数据对照
        /// </summary>
        public class DotDotContrast
        {
            /// <summary>
            /// 代码
            /// </summary>
            public int Code { get; set; }
            /// <summary>
            /// 操作行为（ANDROID）
            /// </summary>
            public string Behavior { get; set; }
            /// <summary>
            /// 序号
            /// </summary>
            public int No { get; set; }
            /// <summary>
            /// 调用方
            /// </summary>
            public string Caller { get; set; }
            /// <summary>
            /// 游戏流程
            /// </summary>
            public string Action { get; set; }
            /// <summary>
            /// 从无序且不规范的XHTML字符串中提取“打点对照”表。
            /// </summary>
            /// <param name="xml"></param>
            /// <returns></returns>
            public static IDictionary<int, DotDotContrast> ParseXml(string xml)
            {
                Dictionary<int, DotDotContrast> dict = new Dictionary<int, DotDotContrast>();
                if (string.IsNullOrEmpty(xml))
                {
                    return dict;
                }
                foreach (Match match in Regex.Matches(xml,
                    @"<tr>[\s\S]+?<td[\s\S]+?>(\d+?)</td>[\s\S]+?<td[\s\S]+?>([\s\S]+?)</td>[\s\S]*?<td[\s\S]*?>([\s\S]*?)</td>[\s\S]*?<td[\s\S]*?>([\s\S]*?)</td>[\s\S]*?<td[\s\S]*?>([\s\S]*?)</td>"))
                {
                    DotDotContrast dot = new DotDotContrast();
                    {
                        string s = Regex.Replace(match.Groups[1].Value, "<.*?>", string.Empty);
                        if (!int.TryParse(s, out int no))
                        {
                            continue;
                        }
                        dot.No = no;
                    }
                    dot.Caller = Regex.Replace(match.Groups[2].Value, "<.*?>", string.Empty);
                    {
                        string s = Regex.Replace(match.Groups[3].Value, "<.*?>", string.Empty);
                        if (!int.TryParse(s, out int code))
                        {
                            continue;
                        }
                        dot.Code = code;
                    }
                    dot.Action = Regex.Replace(match.Groups[4].Value, "<.*?>", string.Empty);
                    dot.Behavior = Regex.Replace(match.Groups[5].Value, "<.*?>", string.Empty);
                    if (string.IsNullOrEmpty(dot.Caller))
                    {
                        dot.Caller = null;
                    }
                    if (string.IsNullOrEmpty(dot.Behavior))
                    {
                        dot.Behavior = null;
                    }
                    if (string.IsNullOrEmpty(dot.Action))
                    {
                        dot.Action = null;
                    }
                    if (!dict.ContainsKey(dot.Code))
                    {
                        dict.Add(dot.Code, dot);
                    }
                }
                return dict;
            }
        }
    }
}
