﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ORM.Common;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using System.Data;

namespace ORM.MainService
{
    /*
    * 本ORM框架属于轻量级别，相当于EF会在效率上提高很多，特别适合不想使用EF，但又不想写ADO.NET操作中封装和解析过程的开发者
    * 本ORM框架对ADO.NET中CRUD操作做了基本的封装，主要使用了泛型和反射技术
    * 本ORM框架能够带来的好处是CRUD的所有操作都是基于实体对象操作，避免增、删、改操作中写SQL语句和参赛封装，同时也避免查询中对象的封装过程
    * 本ORM目前等待完善的地方：1.插入和修改时，默认值的问题、返回标识列的值问题。2.组合主键的问题   3.
    * 
    * 本ORM使用注意的问题：实体类要根据要求添加对应的“特性”（比如：主键特性、标识列特性、非数据库字段特性等）
    */
    public class DBService
    {
        #region Insert

        #region 通过普通SQL语句添加

        /// <summary>
        /// 基于自动生成格式化的SQL语句
        /// </summary>
        /// <param name="model"></param>
        /// <param name="returnIdentity"></param>
        /// <returns></returns>
        public static int SaveByCommonSql(object model, bool returnIdentity = false)
        {
            string msg = ValidateModel(model);
            if (msg != "SUCCESS")
            {
                throw new Exception(model.GetType().Name + "模型验证错误：" + msg);
            }
            //1.准备要组合的sql语句
            StringBuilder sqlFileds = new StringBuilder("INSERT INTO " + model.GetType().Name + "(");
            StringBuilder sqlValues = new StringBuilder(" VALUES (");

            //2.获取实体对象所有的属性
            PropertyInfo[] proArry = model.GetType().GetProperties();

            //3.获取标识列和所有扩展属性（这些字段不会被添加到SQL语句中）
            string identity = GetIdentityColumn(proArry);
            List<string> nonTableFileds = GetNonTableFields(proArry);

            //4.循环添加其他（属性，属性值）到SQL语句中
            foreach (PropertyInfo item in proArry)
            {
                if (item.Name == identity) continue;
                if (nonTableFileds.Contains(item.Name)) continue;

                //获取属性值，过滤空属性
                object columnValue = item.GetValue(model, null);
                //过滤没有赋值的时间，时间属性初始化时未赋值会变为默认最小值（0001/01/01），这个通过null是没法判断出来的
                Type filedType = item.PropertyType;//获取字段的类型
                if (item.PropertyType == typeof(DateTime))
                {
                    DateTime dt;
                    DateTime.TryParse(columnValue.ToString(), out dt);
                    if (dt < SqlDateTime.MinValue.Value) continue;
                }
                sqlFileds.Append(item.Name + ",");

                //在SQL语句中添加字段对应的值（需要考虑“非值类型”添加单引号）
                if (filedType == typeof(string) || filedType == typeof(DateTime))
                {
                    sqlValues.Append("'" + columnValue + "'");
                }
                else
                {
                    sqlValues.Append(columnValue + ",");
                }
            }

            //5.整合sql语句（删除最后一个逗号，并闭合括号）
            string sql1 = sqlFileds.ToString().Trim(new char[] { ',' }) + ")";
            string sql2 = sqlValues.ToString().Trim(new char[] { ',' }) + ")";
            string sql = sql1 + sql2;

            if (returnIdentity)
            {
                sql += ";select @@Identity";
                return Convert.ToInt32(HelperFactory.SQLHelper.GetSingleResult(sql));
            }
            else
            {
                return HelperFactory.SQLHelper.Update(sql);
            }
        }
        #endregion

        #region 通过带参数SQL语句添加

        /// <summary>
        /// 自动生成带参数的SQL语句添加对象
        /// </summary>
        /// <param name="model">实体对象</param>
        /// <returns></returns>
        public static int SaveByParamSql(object model, bool returnIdentity = false)
        {
            string msg = ValidateModel(model);
            if (msg != "SUCCESS")
            {
                throw new Exception(model.GetType().Name + "模型验证错误：" + msg);
            }
            //【1】准备要组合的SQL语句和参数数组
            StringBuilder sqlFileds = new StringBuilder("INSERT INTO " + model.GetType().Name + "(");
            StringBuilder sqlValues = new StringBuilder(" VALUES (");
            List<SqlParameter> paramList = new List<SqlParameter>();

            //【2】获取实体对象所有的属性
            PropertyInfo[] proArray = model.GetType().GetProperties();

            //【3】获取标识列和所有扩展属性（这些字段将不会添加到SQL语句中）
            string identity = GetIdentityColumn(proArray);
            List<string> nonTableFileds = GetNonTableFields(proArray);
            //其他属性，目前这个没有考虑GUID等类型，请学员自行完成

            //【4】循环生成字段和参数名称到SQL语句中            
            foreach (PropertyInfo item in proArray)
            {
                //过滤标识列和非对应字段的属性
                if (item.Name == identity) continue;
                if (nonTableFileds.Contains(item.Name)) continue;

                //获取属性值，过滤空属性
                object columnValue = item.GetValue(model, null);
                if (columnValue == null) continue;

                //过滤没有赋值的时间，时间属性初始化时未赋值会变为默认最小值（0001/01/01），这个通过null是没法判断出来的
                Type filedType = item.PropertyType;//获取字段的类型
                if (item.PropertyType == typeof(DateTime))
                {
                    DateTime dt;
                    DateTime.TryParse(columnValue.ToString(), out dt);
                    if (dt <= SqlDateTime.MinValue.Value)
                        continue;
                }

                //组合SQL语句中字段名称和对应的参数名称
                sqlFileds.Append(item.Name + ",");
                sqlValues.Append("@" + item.Name + ",");

                //将参数添加到参数集合
                paramList.Add(new SqlParameter("@" + item.Name, item.GetValue(model, null)));
            }
            //【5】整合SQL语句（删除最后一个逗号，并闭合括号）
            string sql1 = sqlFileds.ToString().Trim(new char[] { ',' }) + ")";
            string sql2 = sqlValues.ToString().Trim(new char[] { ',' }) + ")";
            string sql = sql1 + sql2;

            //【6】调用普通的通用数据访问类
            if (returnIdentity)//返回带标识列的
            {
                sql += ";select @@Identity";
                return Convert.ToInt32(HelperFactory.SQLHelper.GetSingleResult(sql, paramList.ToArray()));
            }
            else
            {
                return HelperFactory.SQLHelper.Update(sql, paramList.ToArray());
            }
        }

        #endregion

        #region 通过存储过程添加

        public static int SaveByStoreProcedure(object model, string procedureNane)
        {
            string msg = ValidateModel(model);
            if (msg != "SUCCESS")
            {
                throw new Exception(model.GetType().Name + "模型验证错误：" + msg);
            }
            PropertyInfo[] proArray = model.GetType().GetProperties();
            string identity = GetIdentityColumn(proArray);
            List<string> nonTableFiles = GetNonTableFields(proArray);
            List<SqlParameter> paramList = new List<SqlParameter>();
            foreach (PropertyInfo item in proArray)
            {
                if (item.Name == identity) continue;
                if (nonTableFiles.Contains(item.Name)) continue;
                paramList.Add(new SqlParameter("@" + item.Name, item.GetValue(model, null)));
            }
            return HelperFactory.SQLHelper.Update(procedureNane, paramList.ToArray(), true);
        }

        #endregion

        #endregion

        #region Delete

        public static int DeleteByParamSql(object model)
        {
            string msg = ValidateModel(model);
            Type modelType = model.GetType();
            string name = modelType.Name;
            if (msg != "SUCCESS")
            {
                throw new Exception(name + "模型验证错误：" + msg);
            }
            PropertyInfo[] proArray = modelType.GetProperties();
            string primaryKey = GetPrimaryKey(proArray);
            List<SqlParameter> list = new List<SqlParameter>();
            foreach (PropertyInfo property in proArray)
            {
                if (property.Name == primaryKey)
                {
                    list.Add(new SqlParameter("@" + primaryKey, property.GetValue(model, null)));
                    break;
                }
            }
            string sql = "delete from " + name + " where " + primaryKey + " =@" + primaryKey;
            return HelperFactory.SQLHelper.Update(sql, list.ToArray());
        }

        #endregion

        #region Update

        public static int UpdateByParamSql(object model)
        {
            string msg = ValidateModel(model);
            if (!msg.Equals("SUCCESS"))
            {
                throw new Exception(model.GetType().Name + "模型验证错误：" + msg);
            }
            //【1】准备要组合的SQL语句和参数数组
            StringBuilder sqlFileds = new StringBuilder("UPDATE " + model.GetType().Name + " SET ");
            List<SqlParameter> paramList = new List<SqlParameter>();

            //【2】获取实体对象所有的属性
            PropertyInfo[] proArray = model.GetType().GetProperties();

            //【3】获取标识列和所有扩展属性（这些字段将不会添加到SQL语句中）
            string primaryKey = GetPrimaryKey(proArray);
            string identity = GetIdentityColumn(proArray);
            List<string> nonTableFileds = GetNonTableFields(proArray);

            //【4】循环生成字段和参数名称到SQL语句中      
            foreach (PropertyInfo property in proArray)
            {
                if (property.Name == identity) continue;
                if (nonTableFileds.Contains(property.Name)) continue;
                if (primaryKey == property.Name)
                {
                    paramList.Add(new SqlParameter("@" + primaryKey, property.GetValue(model, null)));
                }
                sqlFileds.Append(property.Name + "=@" + property.Name);
                paramList.Add(new SqlParameter("@" + property.Name, property.GetValue(model, null)));
            }
            //组合完整的带参数的SQL语句
            string sql = sqlFileds.ToString();
            sql = sql.Substring(0, sql.Length - 1) + " where " + primaryKey + " =@" + primaryKey;//去掉最后一个逗号，然后再加上where条件

            return HelperFactory.SQLHelper.Update(sql, paramList.ToArray());
        }

        public static int UpdateByStoreProcedure(object model, string procedureName)
        {
            string msg = ValidateModel(model);
            if (!msg.Equals("SUCCESS"))
            {
                throw new Exception(model.GetType().Name + "模型验证错误：" + msg);
            }
            PropertyInfo[] proArray = model.GetType().GetProperties();
            List<SqlParameter> paramList = new List<SqlParameter>();
            string primaryKey = GetPrimaryKey(proArray);
            string identity = GetIdentityColumn(proArray);
            List<string> nonTableFileds = GetNonTableFields(proArray);
            foreach (var property in proArray)
            {
                if (property.Name == primaryKey) continue;
                if (property.Name == identity) continue;
                if (nonTableFileds.Contains(property.Name)) continue;

                paramList.Add(new SqlParameter("@" + property.Name, property.GetValue(model, null)));
            }

            return HelperFactory.SQLHelper.Update(procedureName, paramList.ToArray(), true);
        }

        #endregion

        #region Select

        public static List<T> GetEntitiesFromReader<T>(IDataReader reader) where T : new()
        {
            Type type = typeof(T); //得到当前实体的类型
            PropertyInfo[] proArray = type.GetProperties();//获取属性集合
            List<T> entityList = new List<T>();

            //获取当前查询的所有列名称（注意必须和数据库字段一致）
            List<string> filedNames = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                filedNames.Add(reader.GetName(i).ToLower());
            }
            //循环读取并封装对象
            while (reader.Read())
            {
                T objEntity = new T();
                foreach (PropertyInfo item in proArray)
                {
                    if (filedNames.Contains(item.Name.ToLower()))
                    {
                        item.SetValue(objEntity, Convert.ChangeType(reader[item.Name], item.PropertyType, null));
                    }
                }
                entityList.Add(objEntity);
            }
            reader.Close();
            return entityList;
        }

        #endregion

        #region Common

        //【公共的私有方法】根据属性数组，和列的类型，找到所有列名称
        private static List<string> GetAttributeColumns(PropertyInfo[] proArray, string columnType)
        {
            List<string> columnList = new List<string>();
            for (int i = 0; i < proArray.Length; i++)
            {
                object[] attrs = proArray[i].GetCustomAttributes(false);
                foreach (var item in attrs)
                {
                    if (item.GetType().Name == columnType)
                    {
                        columnList.Add(proArray[i].Name);
                        break;
                    }
                }
            }

            return columnList;
        }

        /// <summary>
        /// 获取标识列
        /// </summary>
        /// <param name="proArray"></param>
        /// <returns></returns>
        private static string GetIdentityColumn(PropertyInfo[] proArray)
        {
            return GetAttributeColumns(proArray, "IdentityAttribute")[0];
        }

        /// <summary>
        /// 获取主键列（这里我们只考虑一个主键的情况，符合主键，学员可以自己改进）
        /// </summary>
        /// <param name="proinfo"></param>
        /// <returns></returns>
        private static string GetPrimaryKey(PropertyInfo[] proArray)
        {
            return GetAttributeColumns(proArray, "PrimaryKeyAttribute")[0];
        }

        /// <summary>
        /// 获取非数据库字段属性
        /// </summary>
        /// <param name="proArray"></param>
        /// <returns></returns>
        private static List<string> GetNonTableFields(PropertyInfo[] proArray)
        {
            return GetAttributeColumns(proArray, "NonTableAttribute");
        }

        /// <summary>
        /// 模型验证
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static string ValidateModel(object model)
        {
            bool isContinue = true;//是否继续执行
            string errorMsg = string.Empty;

            foreach (PropertyInfo property in model.GetType().GetProperties())//遍历所有属性
            {
                if (!isContinue)
                {
                    return errorMsg;//返回错误的验证信息
                }
                //找到当前属性中指定类型的自定义特性
                object[] cusAttribute = property.GetCustomAttributes(typeof(ValidateAtrribute), true);
                //遍历自定义特性
                foreach (var attribute in cusAttribute)
                {
                    ValidateAtrribute att = (ValidateAtrribute)attribute;//转换成父类
                    bool isValid = att.Validate(property.GetValue(model));//调用重写的验证方法
                    if (!isValid)//如果验证不通过
                    {
                        isContinue = false;//不在访问其它属性
                        errorMsg = att.ErrorMessage;
                        break;
                    }
                }
            }
            return "SUCCESS";
        }

        #endregion
    }
}
