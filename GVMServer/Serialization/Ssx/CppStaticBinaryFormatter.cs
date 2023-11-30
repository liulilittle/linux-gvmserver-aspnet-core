namespace GVMServer.Serialization.Ssx
{
    using GVMServer.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Reflection;
    using ValType = GVMServer.Valuetype.Valuetype;

    public class CppStaticBinaryFormatter
    {
        private static string GetMemberName(MemberInfo mi)
        {
            string s = mi.Name;
            if (!string.IsNullOrEmpty(s))
            {
                s = char.ToLower(s[0]) + s.Substring(1);
            }
            return "_" + s;
        }

        private static string CreateDeserializeCodeText(Type type)
        {
            string contents = "bool Deserialize(unsigned char*& buf, int& count) { \r\n";
            contents += "if ((count -= 1) < 0) return false;\r\nif (*buf++ != 0) return false;\r\n";
            foreach (PropertyInfo info in type.GetProperties())
            {
                Type propertyType = ValType.GetUnderlyingType(info.PropertyType);
                if (ValType.IsBasicType(propertyType))
                {
                    if (typeof(long) == propertyType || typeof(DateTime) == propertyType) contents += string.Format("if ((count -= 8) < 0) return false;\r\nthis->{0} = *(long long*)buf;\r\nbuf+=8;\r\n", GetMemberName(info));
                    else if(typeof(ulong) == propertyType) contents += string.Format("if ((count -= 8) < 0) return false;\r\nthis->{0} = *(unsigned long long*)buf;\r\nbuf+=8;\r\n", GetMemberName(info));
                    else if (typeof(int) == propertyType ||
                        typeof(IPAddress) == propertyType) contents += string.Format("if ((count -= 4) < 0) return false;\r\nthis->{0} = *(int*)buf;\r\nbuf+=4;\r\n", GetMemberName(info));
                    else if (typeof(uint) == propertyType ||
                        typeof(IPAddress) == propertyType) contents += string.Format("if ((count -= 4) < 0) return false;\r\nthis->{0} = *(unsigned int*)buf;\r\nbuf+=4;\r\n", GetMemberName(info));
                    else if (typeof(double) == propertyType) contents += string.Format("if ((count -= 8) < 0) return false;\r\nthis->{0} = *(long double*)buf;\r\n buf+=8;\r\n", GetMemberName(info));
                    else if (typeof(float) == propertyType) contents += string.Format("if ((count -= 4) < 0) return false;\r\nthis->{0} = *(float*)buf;\r\nbuf+=4;\r\n", GetMemberName(info));
                    else if (typeof(short) == propertyType) contents += string.Format("if ((count -= 2) < 0) return false;\r\nthis->{0} = *(short*)buf;\r\nbuf+=2;\r\n", GetMemberName(info));
                    else if (typeof(ushort) == propertyType ||
                        typeof(char) == propertyType) contents += string.Format("if ((count -= 2) < 0) return false;\r\nthis->{0} = *(unsigned short*)buf;\r\nbuf+=2;\r\n", GetMemberName(info));
                    else if (typeof(byte) == propertyType) contents += string.Format("if ((count -= 1) < 0) return false;\r\nthis->{0} = *(unsigned char*)buf;\r\nbuf++;\r\n", GetMemberName(info));
                    else if (typeof(sbyte) == propertyType) contents += string.Format("if ((count -= 1) < 0) return false;\r\nthis->{0} = *(char*)buf;\r\nbuf++;\r\n", GetMemberName(info));
                    else if (typeof(Guid) == propertyType) contents += string.Format("if ((count -= 16) < 0) return false;\r\nthis->{0} = *(Guid*)buf;\r\nbuf+=16;\r\n", GetMemberName(info));
                    else if (typeof(string) == propertyType)
                    {
                        contents += "{\r\nif ((count -= 2) < 0) return false;\r\n";
                        contents += "unsigned short len_ = *(unsigned short*)buf;\r\nbuf+=2;\r\n";
                        contents += string.Format("if (len_ == 0xffff) this->Set{0}(NULL, 0xffff);\r\n", info.Name);
                        contents += string.Format("else if (len_ == 0) this->Set{0}(\"\", 0);\r\n", info.Name);
                        contents += ("else {\r\nif ((count -= len_) < 0) return false;\r\nthis->Set" + info.Name + "((char*)buf, len_);\r\nbuf+=len_;\r\n}\r\n");
                        contents += "}\r\n";
                    };
                }
                else
                {
                    Type elementType = Metatype.GetArrayElement(propertyType);
                    if (elementType == null)
                    {
                        contents += $"if ((count - 1) < 0) return false; \r\n";
                        contents += $"if (*buf != 0) {{\r\nbuf++;\r\nif(NULL != this->{GetMemberName(info)}) {{\r\n delete this->{GetMemberName(info)}; \r\n}}\r\nthis->{GetMemberName(info)} = NULL;\r\n}}\r\n";
                        contents += $"else {{\r\nif (NULL == this->{GetMemberName(info)}) ";

                        contents += string.Format("this->{0} = new {1}();\r\n", GetMemberName(info), propertyType.Name);
                        contents += string.Format("this->{0}->Deserialize(buf, count);\r\n", GetMemberName(info));
                    }
                    else
                    {
                        contents += $"if ((count -= 2) < 0) return false; \r\n {{\r\n";
                        contents += "unsigned short xlen_ = *(unsigned short*)buf;\r\nbuf+=2;\r\n";
                        contents += $"if (xlen_ == 0xffff) this->Set{info.Name}(NULL);\r\n";
                        contents += "else {\r\n";
                        if (ValType.IsBasicType(elementType))
                        {
                            contents += string.Format("this->Set{0}(new std::vector<{1}>(xlen_));\r\n", info.Name, ParseTypeName(elementType));
                        }
                        else
                        {
                            contents += string.Format("this->Set{0}(new std::vector<{1}*>(xlen_));\r\n", info.Name, ParseTypeName(elementType));
                        }
                        contents += "for (int idx_ = 0; idx_ < xlen_; idx_++) {\r\n";
                        if (ValType.IsBasicType(elementType))
                        {
                            if (typeof(long) == elementType || typeof(DateTime) == elementType) contents += string.Format("if ((count -= 8) < 0) return false;\r\n(*this->{0})[idx_] = *(long long*)buf;\r\nbuf+=8;\r\n", GetMemberName(info));
                            else if (typeof(ulong) == elementType) contents += string.Format("if ((count -= 8) < 0) return false;\r\n(*this->{0})[idx_] = *(unsigned long long*)buf;\r\nbuf+=8;\r\n", GetMemberName(info));
                            else if (typeof(int) == elementType ||
                                typeof(IPAddress) == elementType) contents += string.Format("if ((count -= 4) < 0) return false;\r\n(*this->{0})[idx_] = *(int*)buf;\r\nbuf+=4;\r\n", GetMemberName(info));
                            else if (typeof(uint) == elementType ||
                                typeof(IPAddress) == elementType) contents += string.Format("if ((count -= 4) < 0) return false;\r\n(*this->{0})[idx_] = *(unsigned int*)buf;\r\nbuf+=4;\r\n", GetMemberName(info));
                            else if (typeof(double) == elementType) contents += string.Format("if ((count -= 8) < 0) return false;\r\n(*this->{0})[idx_] = *(long double*)buf;\r\n buf+=8;\r\n", GetMemberName(info));
                            else if (typeof(float) == elementType) contents += string.Format("if ((count -= 4) < 0) return false;\r\n(*this->{0})[idx_] = *(float*)buf;\r\nbuf+=4;\r\n", GetMemberName(info));
                            else if (typeof(short) == elementType) contents += string.Format("if ((count -= 2) < 0) return false;\r\n(*this->{0})[idx_] = *(short*)buf;\r\nbuf+=2;\r\n", GetMemberName(info));
                            else if (typeof(ushort) == elementType ||
                                typeof(char) == elementType) contents += string.Format("if ((count -= 82 < 0) return false;\r\n(*this->{0})[idx_] = *(unsigned short*)buf;\r\nbuf+=2;\r\n", GetMemberName(info));
                            else if (typeof(byte) == elementType) contents += string.Format("if ((count -= 1) < 0) return false;\r\n(*this->{0})[idx_] = *(unsigned char*)buf;\r\nbuf++;\r\n", GetMemberName(info));
                            else if (typeof(sbyte) == elementType) contents += string.Format("if ((count -= 1) < 0) return false;\r\n(*this->{0})[idx_] = *(char*)buf;\r\nbuf++;\r\n", GetMemberName(info));
                            else if (typeof(Guid) == elementType) contents += string.Format("if ((count -= 16) < 0) return false;\r\n(*this->{0})[idx_] = *(Guid*)buf;\r\nbuf++;\r\n", GetMemberName(info));
                        }
                        else
                        {
                            contents += $"if ((count - 1) < 0) return false; \r\n";
                            contents += $"if (*buf != 0) {{\r\nbuf++;\r\n(*this->{GetMemberName(info)})[idx_]=NULL;\r\ncontinue;\r\n}}\r\n";
                            contents += $"{ParseTypeName(elementType)}* {info.Name}_x_ptr = new {ParseTypeName(elementType)}();\r\n";
                            contents += $"(*this->{GetMemberName(info)})[idx_] = {info.Name}_x_ptr;\r\n";
                            contents += $"if(true != {info.Name}_x_ptr->Deserialize(buf, count)) return false;\r\n";
                        }
                        contents += "}\r\n}\r\n";
                    }
                    contents += "}\r\n";
                }
            }
            contents += "return true; \r\n}\r\n";
            return contents;
        }

        private static string CreateSerializeCodeText(Type type)
        {
            string contents = "void Serialize(std::strstream& s) {\r\n";
            contents += "s.put(NULL == this);\r\n";
            foreach (PropertyInfo info in type.GetProperties())
            {
                Type propertyType = ValType.GetUnderlyingType(info.PropertyType);
                if (ValType.IsBasicType(propertyType))
                {
                    string exp = $"this->{GetMemberName(info)}";
                    if (typeof(long) == propertyType || 
                        typeof(ulong) == propertyType || 
                        typeof(double) == propertyType ||
                        typeof(DateTime) == propertyType)
                        contents += $"s.write((char*)&{exp}, 8);\r\n";
                    else if (typeof(int) == propertyType || typeof(uint) == propertyType ||
                        typeof(float) == propertyType || typeof(IPAddress) == propertyType)
                        contents += $"s.write((char*)&{exp}, 4);\r\n";
                    else if (typeof(short) == propertyType || typeof(ushort) == propertyType || typeof(char) == propertyType)
                        contents += $"s.write((char*)&{exp}, 2);\r\n";
                    else if (typeof(byte) == propertyType || typeof(sbyte) == propertyType)
                        contents += $"s.put((char){exp});\r\n";
                    else if (typeof(Guid) == propertyType)
                        contents += $"s.write((char*)&{exp}, 16);\r\n";
                    else if (typeof(string) == propertyType)
                    {
                        string exp_len = $"this->{GetMemberName(info)}_len";
                        contents += $"s.write((char*)&{exp_len}, 2);\r\n";
                        contents += $"if ({exp_len} > 0 && {exp_len} != 0xffff) \r\ns.write({exp}, {exp_len});\r\n";
                    }
                }
                else
                {
                    Type elementType = Metatype.GetArrayElement(propertyType);
                    string exp = $"this->{GetMemberName(info)}";
                    if (elementType == null)
                    {
                        contents += $"if (NULL == {exp}) \r\ns.put(1); \r\nelse \r\n{exp}->Serialize(s);\r\n";
                    }
                    else
                    {
                        contents += "{\r\n";
                        contents += "unsigned short len_ = 0xffff;\r\n";
                        contents += $"if (NULL != {exp}) {{\r\n";
                        contents += $"unsigned int lsize_ = (unsigned int){exp}->size();\r\n";
                        contents += $"if (lsize_ >= 0xffff) \r\nlsize_ = 0xfffe;\r\n";
                        contents += $"len_ = lsize_; \r\n}}\r\n";
                        contents += $"s.write((char*)&len_, 2);\r\n";
                        contents += "if (len_ > 0 && len_ != 0xffff) {\r\n";
                        contents += "for (int idx_ = 0; idx_ < len_; idx_++) {\r\n";
                        if (ValType.IsBasicType(elementType))
                        {
                            contents += $"{ParseTypeName(elementType)}& item_x_val = (*{exp})[idx_];\r\n";
                            contents += "s.write((char*)&item_x_val, sizeof(item_x_val));\r\n";
                        }
                        else
                        {
                            contents += $"{ParseTypeName(elementType)}* item_x_ptr = (*{exp})[idx_];\r\n";
                            contents += $"if (NULL == item_x_ptr) \r\ns.put(1); \r\n";
                            contents += $"else \r\nitem_x_ptr->Serialize(s);\r\n";
                        }
                        contents += "}\r\n}\r\n}\r\n";
                    }
                }
            }
            contents += "}\r\n";
            return contents;
        }

        private static string ParseTypeName(Type type)
        {
            if (typeof(long) == type || typeof(DateTime) == type) return $"long long";
            else if (typeof(ulong) == type) return $"unsigned long long";
            else if (typeof(int) == type || typeof(IPAddress) == type) return $"int";
            else if (typeof(uint) == type || typeof(IPAddress) == type) return $"unsigned int";
            else if (typeof(double) == type) return $"long double";
            else if (typeof(float) == type) return $"float";
            else if (typeof(short) == type) return $"short";
            else if (typeof(ushort) == type || typeof(char) == type) return $"unsigned short";
            else if (typeof(byte) == type) return $"unsigned char";
            else if (typeof(sbyte) == type) return $"char";
            else if (typeof(string) == type) return $"const char*";
            else if (typeof(Guid) == type) return $"Guid";
            return type.Name;
        }

        private static string GetFullTypeName(Type type)
        {
            if (!type.IsGenericType)
            {
                return type.Name;
            }
            return type.Name.Split("`")[0];
        }

        public static string CreateFormatterText(Type type)
        {
            return CreateFormatterText(type, null);
        }

        public static string CreateFormatterText(Type type, ISet<Type> includes)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            string cppincludes = "#pragma once /*SSX: BinaryFormatter 1.0.1*/ \r\n\r\n#include <string>\r\n#include <vector>\r\n#include <strstream>\r\n\r\n";
            ISet<string> externs = new HashSet<string>();
            string contents = string.Empty;
            if (type.IsGenericType)
            {
                contents += "template<>\r\n";
                contents += "class " + GetFullTypeName(type) + $"<{ParseTypeName(type.GetGenericArguments()[0])}>" + " {\r\n";
            }
            else
            {
                contents += "class " + GetFullTypeName(type) + " { \r\n";
            }
            do
            {
                string extern_ = string.Empty;
                if (type.IsGenericType)
                {
                    extern_ = $"template<typename T>\r\n";
                    extern_ += "class " + GetFullTypeName(type) + ";\r\n";
                }
                else
                {
                    extern_ = $"class {ParseTypeName(type)};\r\n";
                }
                if (!externs.Contains(extern_))
                {
                    externs.Add(extern_);
                    cppincludes += extern_ + "\r\n";
                }
            } while (false);
            contents += "private:\r\n";
            foreach (PropertyInfo info in type.GetProperties())
            {
                Type propertyType = ValType.GetUnderlyingType(info.PropertyType);
                if (ValType.IsBasicType(propertyType))
                {
                    if (info.PropertyType.IsEnum)
                    {
                        string extern_ = $"enum {info.PropertyType.Name};\r\n";
                        if (!externs.Contains(extern_))
                        {
                            externs.Add(extern_);
                            cppincludes += extern_;
                        }
                    }
                    else if (propertyType == typeof(Guid))
                    {
                        string extern_ = $"class {propertyType.Name};\r\n";
                        if (!externs.Contains(extern_))
                        {
                            externs.Add(extern_);
                            cppincludes += extern_;
                        }
                    }
                    if (typeof(long) == propertyType || typeof(DateTime) == propertyType) contents += $"long long {GetMemberName(info)};\r\n";
                    else if (typeof(ulong) == propertyType) contents += $"unsigned long long {GetMemberName(info)};\r\n";
                    else if (typeof(int) == propertyType || typeof(IPAddress) == propertyType) contents += $"int {GetMemberName(info)};\r\n";
                    else if (typeof(uint) == propertyType || typeof(IPAddress) == propertyType) contents += $"unsigned int {GetMemberName(info)};\r\n";
                    else if (typeof(double) == propertyType) contents += $"long double {GetMemberName(info)};\r\n";
                    else if (typeof(float) == propertyType) contents += $"float {GetMemberName(info)};\r\n";
                    else if (typeof(short) == propertyType) contents += $"short {GetMemberName(info)};\r\n";
                    else if (typeof(ushort) == propertyType || typeof(char) == propertyType) contents += $"unsigned short {GetMemberName(info)};\r\n";
                    else if (typeof(byte) == propertyType) contents += $"unsigned char {GetMemberName(info)};\r\n";
                    else if (typeof(sbyte) == propertyType) contents += $"char {GetMemberName(info)};\r\n";
                    else if (typeof(Guid) == propertyType) contents += $"Guid {GetMemberName(info)};\r\n";
                    else if (typeof(string) == propertyType)
                    {
                        contents += $"const char* {GetMemberName(info)};\r\n";
                        contents += $"unsigned short {GetMemberName(info)}_len;\r\n";
                    };
                }
                else
                {
                    Type elementType = Metatype.GetArrayElement(propertyType);
                    if (elementType == null)
                    {
                        string extern_ = string.Empty;
                        if (!propertyType.IsGenericType)
                        {
                            extern_ += $"class {propertyType.Name};\r\n";
                        }
                        else
                        {
                            extern_ += $"template<typename T>\r\n";
                            extern_ += "class " + GetFullTypeName(propertyType) + ";\r\n";
                        }
                        if (!externs.Contains(extern_))
                        {
                            externs.Add(extern_);
                            cppincludes += extern_;
                        }
                        contents += $"{propertyType.Name}* {GetMemberName(info)};\r\n";
                    }
                    else
                    {
                        if (ValType.IsBasicType(elementType))
                        {
                            contents += $"std::vector<{ParseTypeName(elementType)}>* {GetMemberName(info)};\r\n";
                        }
                        else
                        {
                            string extern_ = string.Empty;
                            if (!elementType.IsGenericType)
                            {
                                extern_ = $"class {ParseTypeName(elementType)};\r\n";
                            }
                            else
                            {
                                extern_ += $"template<typename T>\r\n";
                                extern_ += "class " + GetFullTypeName(elementType) + ";\r\n";
                            }
                            if (!externs.Contains(extern_))
                            {
                                externs.Add(extern_);
                                cppincludes += extern_;
                            }
                            contents += $"std::vector<{ParseTypeName(elementType)}*>* {GetMemberName(info)};\r\n";
                        }
                    }
                }
            }
            if (externs.Count <= 1)
            {
                contents = cppincludes + contents;
            }
            else
            {
                contents = cppincludes + "\r\n" + contents;
            }
            contents += "\r\n";
            contents += "public:\r\n";
            string constructor = $"{GetFullTypeName(type)}() {{\r\n";
            string finalize = string.Empty;
            bool fz = false;
            bool fzsz = false;
            foreach (PropertyInfo info in type.GetProperties())
            {
                Type propertyType = ValType.GetUnderlyingType(info.PropertyType);
                if (ValType.IsBasicType(propertyType))
                {
                    string exp = $"this->{GetMemberName(info)}";
                    if (typeof(string) == propertyType)
                    {
                        string exp_len = $"this->{GetMemberName(info)}_len";
                        constructor += $"{exp} = NULL;\r\n";
                        constructor += $"{exp_len} = -1;\r\n";

                        fz = true;
                        fzsz = true;
                        finalize += $"sz={exp};\r\n";
                        finalize += $"{exp}=NULL;\r\n";
                        finalize += $"{exp_len}=-1;\r\n";
                        finalize += $"if (sz != NULL) delete sz;\r\n";
                    }
                    else if (typeof(Guid) == propertyType)
                    {
                        continue;
                    }
                    else
                    {
                        constructor += $"{exp} = 0;\r\n";
                    }
                }
                else
                {
                    fz = true;
                    constructor += $"this->{GetMemberName(info)} = NULL;\r\n";
                    finalize += $"this->Set{info.Name}(NULL);\r\n";
                }
            }
            finalize += "}\r\n";
            constructor += "}\r\n";
            contents += constructor;
            if (fz)
            {
                if (fzsz)
                    finalize = $"~{GetFullTypeName(type)}() {{\r\nconst char* sz = NULL;\r\n" + finalize;
                else
                    finalize = $"~{GetFullTypeName(type)}() {{\r\n" + finalize;
                contents += finalize + "\r\n";
            }
            else
            {
                contents += "\r\n";
            }
            contents += "public:\r\n";
            foreach (PropertyInfo info in type.GetProperties())
            {
                Type propertyType = ValType.GetUnderlyingType(info.PropertyType);
                if (ValType.IsBasicType(propertyType))
                {
                    string segments = string.Empty;
                    if (info.PropertyType.IsEnum)
                    {
                        segments += $"{info.PropertyType.Name} Get{info.Name}() {{\r\nreturn ({info.PropertyType.Name})this->{GetMemberName(info)};\r\n}};\r\n";
                        includes?.Add(info.PropertyType);
                    }
                    else if (typeof(long) == propertyType || typeof(DateTime) == propertyType) segments += $"long long Get{info.Name}() {{\r\nreturn this->{GetMemberName(info)};\r\n}};\r\n";
                    else if (typeof(ulong) == propertyType) segments += $"unsigned long long Get{info.Name}() {{\r\nreturn this->{GetMemberName(info)};\r\n}};\r\n";
                    else if (typeof(int) == propertyType ||
                        typeof(IPAddress) == propertyType) segments += $"int Get{info.Name}() {{\r\nreturn this->{GetMemberName(info)};\r\n}};\r\n";
                    else if (typeof(uint) == propertyType ||
                        typeof(IPAddress) == propertyType) segments += $"unsigned int Get{info.Name}() {{\r\nreturn this->{GetMemberName(info)};\r\n}};\r\n";
                    else if (typeof(double) == propertyType) segments += $"long double Get{info.Name}() {{\r\nreturn this->{GetMemberName(info)};\r\n}};\r\n";
                    else if (typeof(float) == propertyType) segments += $"float Get{info.Name}() {{\r\nreturn this->{GetMemberName(info)};\r\n}};\r\n";
                    else if (typeof(short) == propertyType) segments += $"short Get{info.Name}() {{\r\nreturn this->{GetMemberName(info)};\r\n}};\r\n";
                    else if (typeof(ushort) == propertyType ||
                        typeof(char) == propertyType) segments += $"unsigned short Get{info.Name}() {{\r\nreturn this->{GetMemberName(info)};\r\n}};\r\n";
                    else if (typeof(byte) == propertyType) segments += $"unsigned char Get{info.Name}() {{\r\nreturn this->{GetMemberName(info)};\r\n}};\r\n";
                    else if (typeof(sbyte) == propertyType) segments += $"char Get{info.Name}() {{\r\nreturn this->{GetMemberName(info)};\r\n}};\r\n";
                    else if (typeof(Guid) == propertyType) segments += $"Guid& Get{info.Name}() {{\r\nreturn this->{GetMemberName(info)};\r\n}};\r\n";
                    else if (typeof(string) == propertyType)
                    {
                        string exp = $"this->{GetMemberName(info)}";
                        string exp_len = $"this->{GetMemberName(info)}_len";
                        string code_expr = $"if ({exp_len} == 0xffff) return NULL;\r\n";
                        code_expr += $"else if ({exp_len} == 0) return \"\"; \r\n";
                        code_expr += $"return " + exp + ";\r\n";

                        segments += $"const char* Get{info.Name}() {{\r\n{code_expr}\r\n}};\r\n";
                        segments += $"int Get{info.Name}Length() {{\r\nreturn (({exp_len} == 0xffff) ? -1 : {exp_len});\r\n}};\r\n";
                    }
                    contents += segments;
                }
                else
                {
                    Type elementType = Metatype.GetArrayElement(propertyType);
                    if (elementType == null)
                    {
                        string exp = $"this->{GetMemberName(info)}";
                        contents += $"{propertyType.Name}* Get{info.Name}() {{\r\nreturn {exp};\r\n}}\r\n";
                        contents += $"{propertyType.Name}* Mutable{info.Name}() {{\r\n";
                        contents += $"if ({exp} == NULL) {exp} = new {propertyType.Name}();\r\n";
                        contents += $"return {exp};\r\n}}\r\n";

                        includes?.Add(propertyType);
                    }
                    else
                    {
                        string exp = $"this->{GetMemberName(info)}";
                        if (ValType.IsBasicType(elementType))
                        {
                            contents += $"std::vector<{ParseTypeName(elementType)}>* Get{info.Name}() {{\r\nreturn {exp};\r\n}}\r\n";
                            contents += $"std::vector<{ParseTypeName(elementType)}>* Mutable{info.Name}() {{\r\n";
                            contents += $"if ({exp} == NULL) {exp} = new std::vector<{ParseTypeName(elementType)}>();\r\n";
                        }
                        else
                        {
                            contents += $"std::vector<{ParseTypeName(elementType)}*>* Get{info.Name}() {{\r\nreturn {exp};\r\n}}\r\n";
                            contents += $"std::vector<{ParseTypeName(elementType)}*>* Mutable{info.Name}() {{\r\n";
                            contents += $"if ({exp} == NULL) {exp} = new std::vector<{ParseTypeName(elementType)}*>();\r\n";

                            includes?.Add(elementType);
                        }
                        contents += $"return {exp};\r\n}}\r\n";
                    }
                }
            }
            contents += "\r\n";
            contents += "public:\r\n";
            foreach (PropertyInfo info in type.GetProperties())
            {
                Type propertyType = ValType.GetUnderlyingType(info.PropertyType);
                if (ValType.IsBasicType(propertyType))
                {
                    if (info.PropertyType.IsEnum) contents += $"void Set{info.Name}({info.PropertyType.Name} value) {{\r\nthis->{GetMemberName(info)} = ({ParseTypeName(propertyType)})value;\r\n}};\r\n";
                    else if (typeof(long) == propertyType || typeof(DateTime) == propertyType) contents += $"void Set{info.Name}(long long value) {{\r\nthis->{GetMemberName(info)} = value;\r\n}};\r\n";
                    else if (typeof(ulong) == propertyType) contents += $"void Set{info.Name}(unsigned long long value) {{\r\nthis->{GetMemberName(info)} = value;\r\n}};\r\n";
                    else if (typeof(int) == propertyType || typeof(IPAddress) == propertyType) contents += $"void Set{info.Name}(int value) {{\r\nthis->{GetMemberName(info)} = value;\r\n}};\r\n";
                    else if (typeof(uint) == propertyType || typeof(IPAddress) == propertyType) contents += $"void Set{info.Name}(unsigned int value) {{\r\nthis->{GetMemberName(info)} = value;\r\n}};\r\n";
                    else if (typeof(double) == propertyType) contents += $"void Set{info.Name}(double value) {{\r\nthis->{GetMemberName(info)} = value;\r\n}};\r\n";
                    else if (typeof(float) == propertyType) contents += $"void Set{info.Name}(float value) {{\r\nthis->{GetMemberName(info)} = value;\r\n}};\r\n";
                    else if (typeof(short) == propertyType) contents += $"void Set{info.Name}(short value) {{\r\nthis->{GetMemberName(info)} = value;\r\n}};\r\n";
                    else if (typeof(ushort) == propertyType || typeof(char) == propertyType) contents += $"void Set{info.Name}(unsigned short value) {{\r\nthis->{GetMemberName(info)} = value;\r\n}};\r\n";
                    else if (typeof(byte) == propertyType) contents += $"void Set{info.Name}(unsigned char value) {{\r\nthis->{GetMemberName(info)} = value;\r\n}};\r\n";
                    else if (typeof(sbyte) == propertyType) contents += $"void Set{info.Name}(char value) {{\r\nthis->{GetMemberName(info)} = value;\r\n}};\r\n";
                    else if (typeof(Guid) == propertyType) contents += $"void Set{info.Name}(const Guid& value) {{\r\nthis->{GetMemberName(info)} = value;\r\n}};\r\n";
                    else if (typeof(string) == propertyType)
                    {
                        string exp = $"this->{GetMemberName(info)}";
                        string exp_len = $"this->{GetMemberName(info)}_len";
                        string code_expr = $"void Set{info.Name}(const char* s, unsigned short len = 0xffff) {{\r\n";
                        code_expr += $"if (len == 0xffff && s != NULL) len = (unsigned short)strlen(s);\r\n";
                        code_expr += $"if ({exp_len} == 0 && len == 0) return;\r\n";
                        code_expr += $"if ({exp_len} == 0xffff && len == 0xffff) return;\r\n";
                        code_expr += $"if (NULL != {exp}) delete[] {exp};\r\n";
                        code_expr += $"{exp} = NULL;\r\n{exp_len} = 0xffff; \r\n";
                        code_expr += $"if (len > 0 && len != 0xffff) {exp}=new char[len+1];\r\n";
                        code_expr += $"if (NULL != s && (len > 0 && len != 0xffff)) {{\r\nchar* sz = (char*){exp};\r\nmemcpy(sz, (char*)s, len);\r\nsz[len]='\\x0';\r\n}}\r\n";
                        code_expr += $"{exp_len}=len;\r\n";
                        code_expr += "};\r\n";
                        contents += code_expr;
                    }
                }
                else
                {
                    Type elementType = Metatype.GetArrayElement(propertyType);
                    string exp = $"this->{GetMemberName(info)}";
                    if (elementType == null)
                    {
                        contents += $"void Set{info.Name}({propertyType.Name}* value) {{\r\n";
                        contents += $"{propertyType.Name}* {info.Name}_ptr = {exp};\r\n";
                    }
                    else
                    {
                        if (ValType.IsBasicType(elementType))
                        {
                            contents += $"void Set{info.Name}(std::vector<{ParseTypeName(elementType)}>* value) {{\r\n";
                            contents += $"std::vector<{ParseTypeName(elementType)}>* {info.Name}_ptr = {exp};\r\n";
                        }
                        else
                        {
                            contents += $"void Set{info.Name}(std::vector<{ParseTypeName(elementType)}*>* value) {{\r\n";
                            contents += $"std::vector<{ParseTypeName(elementType)}*>* {info.Name}_ptr = {exp};\r\n";
                        }
                    }
                    contents += $"if (value == {info.Name}_ptr) return;\r\n";
                    contents += $"if ({info.Name}_ptr != NULL) {{\r\n ";
                    contents += $"{exp} = NULL;\r\n";
                    if (elementType != null && !ValType.IsBasicType(elementType))
                    {
                        contents += $"unsigned int len_ = 0;\r\n";
                        contents += $"if ({info.Name}_ptr != NULL) len_ = (unsigned int){info.Name}_ptr->size();\r\n";
                        contents += $"for (unsigned int idx_ = 0; idx_ < len_; idx_++) {{\r\n";
                        contents += $"{ParseTypeName(elementType)}* item_x_ptr = (*{info.Name}_ptr)[idx_];\r\n";
                        contents += $"if (NULL != item_x_ptr) delete item_x_ptr;";
                        contents += $"}}\r\n";
                    }
                    contents += $"delete {info.Name}_ptr;\r\n}}\r\n";
                    contents += $"{exp} = value;}}\r\n";
                }
            }
            contents += "\r\n";
            contents += "public:\r\n";
            contents += CreateDeserializeCodeText(type);
            contents += CreateSerializeCodeText(type);
            contents += "};\r\n";
            return contents;
        }
    }
}
