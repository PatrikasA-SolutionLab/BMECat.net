/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */
using System;

namespace BMECat.net
{ 
    public enum LanguageCodes
    {
        /// <summary>
        /// Bulgarian
        /// </summary>
        BUL,

        /// <summary>
        /// Czech
        /// </summary>
        CES,

        /// <summary>
        /// Danish
        /// </summary>
        DAN,

        /// <summary>
        /// German
        /// </summary>
        DEU,

        /// <summary>
        /// English
        /// </summary>
        ENG,

        /// <summary>
        /// Finnish
        /// </summary>
        FIN,

        /// <summary>
        /// French
        /// </summary>
        FRA,

        /// <summary>
        /// Hungarian
        /// </summary>
        HUN,

        /// <summary>
        /// Italian
        /// </summary>
        ITA,

        /// <summary>
        /// Japanese
        /// </summary>
        JPN,

        /// <summary>
        /// Dutch
        /// </summary>
        NLD,

        /// <summary>
        /// Norwegian
        /// </summary>
        NOR,

        /// <summary>
        /// Polish
        /// </summary>
        POL,

        /// <summary>
        /// Portuguese
        /// </summary>
        POR,

        /// <summary>
        /// Romanian
        /// </summary>
        RON,

        /// <summary>
        /// Russian
        /// </summary>
        RUS,

        /// <summary>
        /// Slovak
        /// </summary>
        SLK,

        /// <summary>
        /// Spanish
        /// </summary>
        SPA,

        /// <summary>
        /// Swedish
        /// </summary>
        SWE,

        /// <summary>
        /// Turkish
        /// </summary>
        TUR,

        /// <summary>
        /// Chinese
        /// </summary>
        ZHO,
    }


    public static class LanguageCodesExtensions
    {
        public static LanguageCodes? FromString(this LanguageCodes _c, string s)
        {
            if (Enum.TryParse(s, true, out LanguageCodes result))
            {
                return result;
            }

            return null;
        } // !FromString()


        public static string EnumToString(this LanguageCodes c)
        {
            return c.ToString("g").ToLower();
        } // !ToString()
    }
}
