namespace GVMServer.Web.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Text.RegularExpressions;
    using GVMServer.W3Xiyou.Docking;
    using GVMServer.Web.Service;
    using GVMServer.Web.Utilities;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json.Linq;

    [Produces("application/json")]
    [Route("api/[controller]")]
    public class VersionController : Controller
    {
        public class VersionContext
        {
            public int metatype;
            public string sid;
            public string dept;
            public string instanceid;
            public IList<string> dependenciesinstances;
            public JObject data;

            public override string ToString()
            {
                return XiYouSerializer.SerializableJson(this);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ISet<string> filters = new HashSet<string>()
        {
            "dept",
            "sid",
            "servers",
            "metatype",
            "dependenciesinstances",
            "instanceid",
        };

        public enum VersionUpdateError
        {
            VersionUpdateError_Success,
            VersionUpdateError_MissingDepartmentFieldOrItsValueIsEmpty,                                             // 缺失部门字段或者其值为空。
            VersionUpdateError_MetatypeItsValueIsZeroOrUndefined,                                                   // 元类型其值为0或者未定义。
            VersionUpdateError_TryToGetAnInstanceOfRedisButNotReady,                                                // 尝试获取一个Redis实例但并未准备就绪。
            VersionUpdateError_MissingServerIdFieldOrItsValueIsEmptyOrZero,                                         // 缺失服务器ID字段或者其值为空。
            VersionUpdateError_MissingInstanceIdFieldOrItsValueIsEmpty,                                             // 缺失实例字段或者其值为空。
            VersionUpdateError_WriteToMemoryCacheTimesAnUnsolvedErrorWasFound,                                      // 写入内存缓冲时发现一个未解错误。
            VersionUpdateError_TheMemoryCacheWasNotSuccessfullyWrittenThisTimesTransactionsBeBegingRollback,        // 本地未写入内存缓冲正在进行事务回滚。
        }

        public enum VersionLookupError
        {
            VersionUpdateError_Success,
            VersionLookupError_MissingDepartmentFieldOrItsValueIsEmpty,                                             // 缺失部门字段或者其值为空。
            VersionLookupError_TryToGetAnInstanceOfRedisButNotReady,                                                // 尝试获取一个Redis实例但并未准备就绪。
            VersionLookupError_MissingServerIdFieldOrItsValueIsEmptyOrZero,                                         // 缺失服务器ID字段或者其值为空。
            VersionLookupError_ReadInMemoryCacheTimesAnUnsolvedErrorWasFound,                                       // 读入内存缓冲时发现一个未解错误。
            VersionLookupError_VersionInformationWithThisDepartmentAndServerIdCouldNotBeFound,                      // 找不到与此部分与服务器ID对应的版本信息。
        }

        private int ConvertToInt32(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return 0;
            }

            s = (s ?? string.Empty).TrimStart().TrimEnd();
            if (string.IsNullOrEmpty(s))
            {
                return 0;
            }

            if (!int.TryParse(s, out int n))
            {
                return 0;
            }

            return n;
        }

        private IPAddress ConvertToAddress(string s)
        {
            IPAddress address = null;
            try
            {
                if (!string.IsNullOrEmpty(s))
                {
                    s = s.TrimStart().TrimEnd();
                    if (!string.IsNullOrEmpty(s)) // 只匹配IPV4（AF_INET协议簇格式的地址）。
                    {
                        Match match = Regex.Match(s, @"^[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}$");
                        if (match.Success && !IPAddress.TryParse(match.Value, out address))
                        {
                            address = null;
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
            return address ?? IPAddress.Any;
        }

        private IList<string> GetDependenciesInstances(string s, ICollection<string> ig)
        {
            IList<string> addresses = new List<string>();
            if (string.IsNullOrEmpty(s))
            {
                goto RETN_0;
            }

            s = s.TrimStart().TrimEnd();
            if (string.IsNullOrEmpty(s))
            {
                goto RETN_0;
            }

            HashSet<string> hashset = new HashSet<string>();
            try
            {
                JArray jArray = XiYouSerializer.DeserializeJson<JArray>(s);
                if (jArray == null)
                {
                    goto RETN_0;
                }

                var tokens = jArray.Values();
                foreach (JToken jToken in tokens)
                {
                    string instanceid = (jToken?.ToString() ?? string.Empty).TrimStart().TrimEnd();
                    if (string.IsNullOrEmpty(instanceid))
                    {
                        continue;
                    }

                    if (hashset.Add(instanceid))
                    {
                        if (null != ig && ig.Contains(instanceid))
                        {
                            continue;
                        }

                        addresses.Add(instanceid);
                    }
                }
            }
            catch (Exception)
            {
                goto RETN_0;
            }

        RETN_0:
            return addresses;
        }

        [HttpGet("Update")]
        public GenericResponse<object, VersionUpdateError> Update()
        {
            var context = base.HttpContext;
            string dept = (context?.GetQueryValue("dept") ?? string.Empty).TrimStart().TrimEnd();
            if (string.IsNullOrEmpty(dept))
            {
                return new GenericResponse<object, VersionUpdateError>() { Code = VersionUpdateError.VersionUpdateError_MissingDepartmentFieldOrItsValueIsEmpty };
            }

            int metatype = ConvertToInt32(context.GetQueryValue("metatype"));
            if (0 == metatype)
            {
                return new GenericResponse<object, VersionUpdateError>() { Code = VersionUpdateError.VersionUpdateError_MetatypeItsValueIsZeroOrUndefined };
            }

            var servers = GetDependenciesInstances(context.GetQueryValue("sid"), null);
            if (servers == null || servers.Count <= 0)
            {
                return new GenericResponse<object, VersionUpdateError>() { Code = VersionUpdateError.VersionUpdateError_MissingServerIdFieldOrItsValueIsEmptyOrZero };
            }

            string instanceid = (context.GetQueryValue("instanceid") ?? string.Empty).TrimStart().TrimEnd();
            if (string.IsNullOrEmpty(instanceid))
            {
                return new GenericResponse<object, VersionUpdateError>() { Code = VersionUpdateError.VersionUpdateError_MissingInstanceIdFieldOrItsValueIsEmpty };
            }

            VersionContext version = new VersionContext();
            version.dept = dept;
            version.metatype = metatype;
            version.data = new JObject();
            version.instanceid = instanceid;
            version.dependenciesinstances = GetDependenciesInstances(context.GetQueryValue("dependenciesinstances"), new string[] { version.instanceid });
            version.data["servers"] = JToken.FromObject(servers);

            var collection = context?.Request?.Query;
            if (collection != null && collection.Count > 0)
            {
                foreach (var kv in collection)
                {
                    string key = (kv.Key ?? string.Empty).TrimStart().TrimEnd();
                    if (filters.Contains(key))
                    {
                        continue;
                    }

                    string value = kv.Value.ToString() ?? string.Empty;
                    if (value == "true" || value == "True")
                    {
                        version.data[key] = true;
                    }
                    else if (kv.Value == "false" || value == "False")
                    {
                        version.data[key] = false;
                    }
                    else if (kv.Value == "0" | double.TryParse(kv.Value, out double n))
                    {
                        version.data[key] = n;
                    }
                    else
                    {
                        if (DateTime.TryParse(kv.Value, out DateTime dateTime))
                        {
                            version.data[key] = dateTime;
                        }
                        else if (TimeSpan.TryParse(kv.Value, out TimeSpan timeSpan))
                        {
                            version.data[key] = timeSpan;
                        }
                        else if (kv.Value != "null" && value != "undefined")
                        {
                            var match = Regex.Match(kv.Value, @"^\[[\s\S]+\]$");
                            if (!match.Success)
                            {
                                match = Regex.Match(kv.Value, @"^\{[\s\S]+\}$");
                            }

                            try
                            {
                                if (!match.Success)
                                {
                                    version.data[key] = value;
                                }
                                else
                                {
                                    JToken jToken = JToken.Parse(match.Value);
                                    version.data[key] = jToken;
                                }
                            }
                            catch (Exception)
                            {
                                version.data[key] = value;
                            }
                        }
                    }
                }
            }

            var response = new GenericResponse<object, VersionUpdateError>() { Code = VersionUpdateError.VersionUpdateError_Success };
            if (!EntryService.TryGetRedisClient((redis) =>
            {
                try
                {
                    if (1 == metatype)
                    {
                        IDictionary<string, string> values = new Dictionary<string, string>();
                        foreach (string server in servers)
                        {
                            string key = dept + ".version." + server;
                            version.sid = server;
                            values[key] = version.ToString();
                        }

                        version.sid = string.Empty;
                        using (var pipeline = redis.CreatePipeline())
                        {
                            pipeline.QueueCommand(r => r.SetAll(values));
                            pipeline.Flush();
                        }
                    }
                    else
                    {
                        string key = dept + ".version-" + instanceid;
                        if (!redis.Set(key, version.ToString()))
                        {
                            response.Code = VersionUpdateError.VersionUpdateError_TheMemoryCacheWasNotSuccessfullyWrittenThisTimesTransactionsBeBegingRollback;
                        }
                    }
                }
                catch (Exception)
                {
                    response.Code = VersionUpdateError.VersionUpdateError_WriteToMemoryCacheTimesAnUnsolvedErrorWasFound;
                }
            }))
            {
                return new GenericResponse<object, VersionUpdateError>() { Code = VersionUpdateError.VersionUpdateError_TryToGetAnInstanceOfRedisButNotReady };
            }
            return response;
        }

        private static T Bs2t<T>(string s, out JToken token) where T : class
        {
            token = null;
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }

            try
            {
                token = JToken.Parse(s);
                return token.ToObject<T>();
            }
            catch (Exception)
            {
                return null;
            }
        }

        [HttpGet("Lookup")]
        public object Lookup()
        {
            var context = base.HttpContext;
            string dept = (context.GetQueryValue("dept") ?? string.Empty).TrimStart().TrimEnd();
            if (string.IsNullOrEmpty(dept))
            {
                return new GenericResponse<object, VersionLookupError>() { Code = VersionLookupError.VersionLookupError_MissingDepartmentFieldOrItsValueIsEmpty };
            }

            string sid = (context.GetQueryValue("sid") ?? string.Empty).TrimStart().TrimEnd();
            if ("0" == sid)
            {
                sid = string.Empty;
            }

            if (string.IsNullOrEmpty(sid))
            {
                return new GenericResponse<object, VersionLookupError>() { Code = VersionLookupError.VersionLookupError_MissingServerIdFieldOrItsValueIsEmptyOrZero };
            }

            var response = new GenericResponse<object, VersionLookupError>() { Code = VersionLookupError.VersionUpdateError_Success };
            JArray instances = new JArray();
            response.Tag = instances;
            if (!EntryService.TryGetRedisClient((redis) =>
            {
                try
                {
                    do
                    {
                        VersionContext version = Bs2t<VersionContext>(redis.Get<string>(dept + ".version." + sid), out JToken versiontoken); // 聚合根（aggregate root）
                        if (version == null)
                        {
                            response.Code = VersionLookupError.VersionLookupError_VersionInformationWithThisDepartmentAndServerIdCouldNotBeFound;
                            break;
                        }
                        else
                        {
                            instances.Add(versiontoken);
                        }

                        var dependenciesinstances = version.dependenciesinstances;
                        if (dependenciesinstances != null && dependenciesinstances.Count > 0) // 依赖实例列表，以下逐行扫描检索依赖实例的版本信息。
                        {
                            foreach (string instanceid in dependenciesinstances)
                            {
                                if (string.IsNullOrEmpty(instanceid))
                                {
                                    continue;
                                }

                                try
                                {
                                    string fkVersion = redis.Get<string>(dept + ".version-" + instanceid); // 通用子类
                                    if (string.IsNullOrEmpty(fkVersion))
                                    {
                                        continue;
                                    }

                                    instances.Add(JToken.Parse(fkVersion));
                                }
                                catch (Exception)
                                {
                                    break;
                                }
                            }
                        }
                    } while (false);
                }
                catch (Exception)
                {
                    response.Code = VersionLookupError.VersionLookupError_ReadInMemoryCacheTimesAnUnsolvedErrorWasFound;
                }
            }))
            {
                return new GenericResponse<object, VersionLookupError>() { Code = VersionLookupError.VersionLookupError_TryToGetAnInstanceOfRedisButNotReady };
            }

            return response;
        }
    }
}