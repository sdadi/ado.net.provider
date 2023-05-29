using System;
using System.Configuration;
using System.Data;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using System.Xml;
using System.Xml.XPath;
using System.Collections;
using System.Collections.Specialized;
using System.IO;

namespace ado.net.provider
{
    /// <summary>
    /// Summary description for StoredProcedure.
    /// </summary>
    public class StoredProcedureParameters : NameObjectCollectionBase
    {
        // private DictionaryEntry _de = new DictionaryEntry();

        public StoredProcedureParameters()
        {
        }
        public object this[int i]
        {
            get { return (this.BaseGet(i)); }
            set { this.BaseSet(i, value); }
        }
        public object this[string key]
        {
            get { return (this.BaseGet(key)); }
            set { this.BaseSet(key, value); }
        }
        public void Add(string key, object value)
        {
            if (this[key] == null)
                this.BaseAdd(key, value);
            else if ((value != null) && (value.ToString() != "Null"))
                this[key] = value;
        }
        public void Clear()
        {
            base.BaseClear();
        }
    }
}
