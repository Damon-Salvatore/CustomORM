﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ORM.Common
{
    /// <summary>
    /// 基于特性验证的父类（参考了MVC中的验证类）
    /// </summary>
    public abstract class ValidateAtrribute : Attribute
    {
        /// <summary>
        /// 给用户显示的实体属性名
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        ///  是否验证通过
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        ///  验证返回的信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 抽象的验证方法
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract bool Validate(object value);
    }

    /// <summary>
    /// 非空验证
    /// </summary>
    public class RequiredAttribute : ValidateAtrribute
    {
        /// <summary>
        /// 构造方法：给用户要显示的实体属性名
        /// </summary>
        /// <param name="displayName"></param>
        public RequiredAttribute(string displayName)
        {
            DisplayName = displayName;
        }

        public override bool Validate(object value)
        {
            if (value == null || value.ToString().Trim().Length == 0)
            {
                IsValid = false;
                ErrorMessage = $"{DisplayName}不能为空！";
            }
            else
            {
                IsValid = true;
            }
            return IsValid;
        }
    }

    /// <summary>
    /// 整数范围验证
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class RangeAttribute : ValidateAtrribute
    {
        private int min = 0;
        private int max = 0;

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="displayName">给用户要显示的实体属性名</param>
        /// <param name="minValue">范围中的最整数值</param>
        /// <param name="maxValue">范围中的最大整数值</param>
        public RangeAttribute(string displayName, int minValue, int maxValue)
        {
            DisplayName = displayName;
            min = minValue;
            max = maxValue;
        }
        public override bool Validate(object value)
        {
            if (value == null || value.ToString().Trim().Length == 0)
            {
                IsValid = false;
                ErrorMessage = $"{DisplayName}不能为空！";
            }
            else
            {
                //格式验证
                int num = int.Parse(value.ToString());
                if (num < min || num > max)
                {
                    IsValid = false;
                    ErrorMessage = $"{DisplayName}的值必须在{min}和{max}之间";
                }
                else
                {
                    IsValid = false;
                }
            }
            return IsValid;
        }
    }
    /// <summary>
    /// 字符串固定长度
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class FixedLengthAttribute : ValidateAtrribute
    {
        private int fixedLength = 0;

        public FixedLengthAttribute(string displayName, int fixedLength)
        {
            DisplayName = displayName;
            this.fixedLength = fixedLength;
        }

        public override bool Validate(object value)
        {
            //先验证非空
            if (value == null || value.ToString().Trim().Length == 0)
            {
                IsValid = false;
                ErrorMessage = $"{DisplayName}不能为空！";
            }
            else
            {
                //判断是不是固定长度
                if (value.ToString().Length != fixedLength)
                {
                    IsValid = false;
                    ErrorMessage = $"{DisplayName}的长度必须是{fixedLength}位！";
                }
                else
                {
                    IsValid = true;
                }
            }
            return IsValid;
        }
    }

    /// <summary>
    /// 字符串长度范围验证
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class StringLengthAttribute : ValidateAtrribute
    {
        private int minLength = 0;
        private int maxLength = 0;

        public StringLengthAttribute(string displayName, int min, int max)
        {
            DisplayName = displayName;
            minLength = min;
            maxLength = max;
        }

        public override bool Validate(object value)
        {
            if (value == null || value.ToString().Trim().Length == 0)
            {
                IsValid = false;
                ErrorMessage = $"{DisplayName}不能为空！";
            }
            else
            {
                int length = value.ToString().Trim().Length;
                if (length < minLength || length > maxLength)
                {
                    IsValid = false;
                    ErrorMessage = $"{DisplayName}的长度必须在{minLength}和{maxLength}之间！";
                }
            }
            return IsValid;
        }
    }

    /// <summary>
    /// 电子邮件验证
    /// </summary>
    public class EmailAttribute : ValidateAtrribute
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="displayName">给用户要显示的实体属性名</param> 
        public EmailAttribute(string displayName)
        {
            DisplayName = displayName;
        }
        public override bool Validate(object value)
        {
            //先非空验证
            if (value == null || value.ToString().Trim().Length == 0)
            {
                IsValid = false;
                ErrorMessage = $"{base.DisplayName}不能为空！";
            }
            else
            {
                if (!CommonRegexValidate.IsEmail(value.ToString()))
                {
                    IsValid = false;
                    ErrorMessage = $"{DisplayName}的格式不正确！";
                }
                else
                {
                    IsValid = true;
                }
            }
            return IsValid;
        }
    }
    // 其他验证...


    /// <summary>
    /// 通用正则表达式
    /// </summary>
    public class RegularExpressionAttribute : ValidateAtrribute
    {
        private string regExp = string.Empty;

        public RegularExpressionAttribute(string displayName, string regularExpression)
        {
            DisplayName = displayName;
            regExp = regularExpression;
        }

        public override bool Validate(object value)
        {
            //先非空验证
            if (value == null || value.ToString().Trim().Length == 0)
            {
                IsValid = false;
                ErrorMessage = $"{base.DisplayName}不能为空！";
            }
            else
            {
                Regex regex = new Regex(regExp);
                if (!regex.IsMatch(value.ToString()))
                {
                    IsValid = false;
                    ErrorMessage = $"请输入格式正确的{DisplayName}!";
                }
                else
                {
                    IsValid = true;
                }
            }
            return true;
        }
    }

}
