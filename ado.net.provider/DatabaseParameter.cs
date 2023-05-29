using System.Data.Common;
namespace ado.net.provider;

public sealed class DatabaseParameter
{
    public string Name {get;set;}
    public object Value { get; set; }
    public DbType DbType {get; set; }
    public ParameterDirection Direction {get; set;}
}
