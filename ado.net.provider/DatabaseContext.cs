using System;
using System.Configuration;
using System.Data;
using System.Data.SqlTypes;
using System.Data.Common;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Xml.Schema;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.Serialization;

namespace ado.net.provider
{
    /// <summary>
    /// Wrapper for ADO.NET based Database operations 
    /// </summary>
	public class DatabaseContext : IDisposable 
    {
        private bool Disposed = false;
		private int _UserID;
		public int UserID
		{
			get { return _UserID; }
			set { _UserID = value; }
		}
		
        
        public static string[] ParameterQuery = {
										   "SELECT syscolumns.name, syscolumns.xtype AS type, syscolumns.prec, syscolumns.scale, syscolumns.colid FROM sysobjects INNER JOIN syscolumns ON syscolumns.id = sysobjects.id WHERE sysobjects.name = '{0}' ORDER BY syscolumns.colid",
										   "SELECT syscolumns.name, syscolumns.xtype AS type, syscolumns.prec, syscolumns.scale, syscolumns.colid FROM sysobjects INNER JOIN syscolumns ON syscolumns.id = sysobjects.id WHERE sysobjects.name = '{0}' ORDER BY syscolumns.colid"
									   }; 
        public string CommandText;
        public string ObjectName = "Data";
        private string _ConnectionString;
        public string ConnectionString
        {
            get { return (_ConnectionString); }
            set { _ConnectionString = value; }
        }
        public DbParameterCollection Parameters;

        private void initialize()
        {
            Parameters = new DbParameterCollection();
        }
        public DatabaseContext()
        {
            initialize();
        }
        public DatabaseContext(string connectionString)
        {
            ConnectionString = connectionString;
            initialize();
        }
        public DatabaseContext(string connectionString, string objectName)
        {
            ConnectionString = connectionString;
            ObjectName = objectName;
            initialize();
        }
        public DatabaseContext(string connectionString, string objectName, string commandText)
        {
            ConnectionString = connectionString;
            ObjectName = objectName;
            CommandText = commandText;
            initialize();
        }
		
        private int _CommandTimeout;
        private bool _CommandTimeoutExplicit;
        public int CommandTimeout
        {
            get
            {
                if ((_CommandTimeout == 0) && (!_CommandTimeoutExplicit))
                {
                    if (ConfigurationManager.AppSettings["ado.net.provider.CommandTimeout"] != null)
                        _CommandTimeout = Convert.ToInt32(ConfigurationManager.AppSettings["ado.net.provider.CommandTimeout"]);
                    else
                        _CommandTimeout = 30;
                }
                return _CommandTimeout;
            }
            set
            {
                _CommandTimeout = value;
                _CommandTimeoutExplicit = true;
            }
        }

        private string _FactoryClass;
        public string FactoryClass
        {
            get
            {
                if (_FactoryClass == null)
                {
                    _FactoryClass = ConfigurationManager.AppSettings["ado.net.provider.DbProviderFactory"];
                    if (_FactoryClass == null) _FactoryClass = "System.Data.SqlClient";
                }
                return _FactoryClass;
            }
            set { _FactoryClass = value; }
        }
        public string DbProviderClass
        {
            get
            {
                string[] ns = FactoryClass.Split(".".ToCharArray());
                return ns[ns.Length - 1];
            }
        }
        private DbProviderFactory _Factory;
        public DbProviderFactory Factory
        {
            get
            {
                if (_Factory == null)
                {
                    _Factory = DbProviderFactories.GetFactory(FactoryClass);
                }
                return _Factory;
            }
        }
        private DbConnection _Connection;
        public DbConnection Connection
        {
            get
            {
                if (_Connection == null)
                {
                    _Connection = Factory.CreateConnection();
                    _Connection.ConnectionString = ConnectionString;
                }
                return _Connection;
            }
            set
            {
                _Connection = value;
            }
        }
        // public SqlCommand CommandMssql
        public System.Data.Common.DbCommand Command
        {
            get
            {
                Connection.Open();
                DbCommand _Command = Factory.CreateCommand();
                _Command.CommandText = CommandText;
                _Command.CommandType = CommandType.StoredProcedure;
                _Command.Connection = Connection;
                _Command.CommandTimeout = CommandTimeout;

                string paramType;
                for (int i = 0; i < Parameters.Count; i++)
                {

                    paramType = (Parameters[i] == null) ? "null" : Parameters[i].GetType().ToString();
                    switch (paramType)
                    {
                        case "null":
                            break;
                        default:
                            DbParameter _Parameter = Factory.CreateParameter();
                            _Parameter.ParameterName = "@" + this.Parameters.Keys[i];
                            _Parameter.Value = this.Parameters[i];
                            _Command.Parameters.Add(_Parameter);
                            // theCommand.Parameters.AddWithValue("@" + this.Parameters.Keys[i], this.Parameters[i]);
                            break;
                    }

                }

                return (_Command);
            }
        }

        public DataSet Execute()
        {
            DataSet resultSet = new DataSet(ObjectName + "Collection");
            return Execute(resultSet, ObjectName + "Item");
        }
        public DataSet Execute(DataSet resultSet)
        {
            return Execute(resultSet, ObjectName + "Item");
        }
        public DataSet Execute(DataSet resultSet, string tableName)
        {
            try
            {
                DbDataAdapter Adaptor = Factory.CreateDataAdapter();
                Adaptor.SelectCommand = Command;
                Adaptor.Fill(resultSet, tableName);
            }
            catch (Exception err)
            {
                throw new Exception("Error executing stored procedure '" + CommandText + "'.", err);
            }
            finally
            {
                Connection.Close();
            }
			return (AutoName.IsTrue) ? SmartRename(resultSet) : resultSet;
        }
        public int ExecuteScalar()
        {
            int identity;
            try
            {
                identity = Convert.ToInt32(Command.ExecuteScalar());
            }
            catch (Exception err)
            {
                throw new Exception("Error executing stored procedure '" + CommandText + "'.", err);
            }
            finally
            {
                Connection.Close();
            }
            return identity;
        }
        public int ExecuteNonQuery()
        {
            int rowsAffected = 0;
            try
            {
                rowsAffected = Command.ExecuteNonQuery();
            }
            catch (Exception err)
            {
                throw new Exception("Error executing stored procedure '" + CommandText + "'.", err);
            }
            finally
            {
                Connection.Close();
            }
            return rowsAffected;
        }

        public DataSet GetParameters()
        {
            DataSet ds = new DataSet("ParameterCollection");
            return GetParameters(ds);
        }
        public DataSet GetParameters(DataSet dataSet)
        {
            DbDataAdapter Adaptor = Factory.CreateDataAdapter();
            Adaptor.SelectCommand = Factory.CreateCommand();
            Adaptor.SelectCommand.Connection = Factory.CreateConnection();
            Adaptor.SelectCommand.Connection.ConnectionString = ConnectionString.ConnectionString;
            Adaptor.SelectCommand.CommandType = CommandType.Text;
            Adaptor.SelectCommand.CommandText = String.Format(ParameterQuery[0], CommandText);
            Adaptor.Fill(dataSet, "ParameterItem");
            return dataSet;
        }

        private string spParametersMssql
        {
            get
            {
                return ("SELECT syscolumns.name, syscolumns.xtype AS type, syscolumns.prec, syscolumns.scale, syscolumns.colid FROM sysobjects INNER JOIN syscolumns ON syscolumns.id = sysobjects.id WHERE sysobjects.name = '" + this.CommandText + "' ORDER BY syscolumns.colid");
            }
        }

        // Implement IDisposable.
        public void Dispose()
        {
            Dispose(true);
            // Remove from Finalization queue to prevent finalization code from executing a second time.
            GC.SuppressFinalize(this);
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the 
        // runtime from inside the finalizer and you should not reference 
        // other objects. Only unmanaged resources can be disposed.
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.Disposed)
            {
                // If disposing equals true, dispose all managed and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    if ((_Connection != null) && (_Connection.State != ConnectionState.Closed))
                        _Connection.Close();
                    Connection.Dispose();
                }
                // Release unmanaged resources. If disposing is false, only the following code is executed.
            }
            Disposed = true;
        }

	}
}
