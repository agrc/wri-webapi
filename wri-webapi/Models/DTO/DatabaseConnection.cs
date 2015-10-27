using System.Data.SqlClient;

namespace wri_webapi.Models.DTO
{
    public class DatabaseConnection
    {
        private readonly bool _open;
        private readonly SqlConnection _connection;

        public DatabaseConnection(bool open, SqlConnection connection)
        {
            _open = open;
            _connection = connection;
        }

        public bool Open
        {
            get { return _open; }
        }

        public SqlConnection Connection
        {
            get { return _connection; }
        }
    }
}