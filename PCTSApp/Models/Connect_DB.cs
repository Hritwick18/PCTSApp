using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Data.SqlClient;

/// <summary>
/// Summary description for Connect_DB
/// </summary>
public class Connect_DB
{
    //private static string Con_Asha = ConfigurationManager.ConnectionStrings["AshaSoft"].ToString();
    private static string Con_Rajmedical = ConfigurationManager.ConnectionStrings["Rajmedicalcon"].ToString();
    private static string Con_cnaa = ConfigurationManager.ConnectionStrings["cnaaCon"].ToString();
    //private static string Con_AshaReport = ConfigurationManager.ConnectionStrings["AshaSoftReport"].ToString();

    public Connect_DB()
    {
        //
        // TODO: Add constructor logic here
        //
    }

    public static string SetConnection(string ConName)
    {
        string ConnectionString = "";
        if (ConName.ToLower() == "cnaa")
        {
            ConnectionString = Convert.ToString(Con_cnaa);
        }
        else if (ConName.ToLower() == "rajmedical")
        {
            ConnectionString = Convert.ToString(Con_Rajmedical);
        }

        //else if (ConName.ToLower() == "AshaSoftReport".ToLower())
        //{
        //    ConnectionString = Convert.ToString(Con_AshaReport);
        //}
        //else
        //{
        //    ConnectionString = Convert.ToString(Con_Asha);
        //}

        SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(ConnectionString);
        return builder.ConnectionString;
    }

    private static void fill(SqlCommand cmd, SqlParameter[] pm)
    {
        for (int i = 0; i <= pm.Length - 1; i++)
        {
            cmd.Parameters.Add(pm[i]);
        }
    }


    public static DataSet DataSet(string query, SqlParameter[] p, string ConName, int TimeOut = 2000)
    {
        SqlConnection cn = new SqlConnection(SetConnection(ConName));
        DataSet ds = new DataSet();
        SqlCommand cmd = new SqlCommand();
        try
        {
            cmd.CommandText = query;
            cmd.Connection = cn;
            cmd.Parameters.AddRange(p);
            SqlDataAdapter sd = new SqlDataAdapter();
            //fill(cmd, p);
            cmd.CommandTimeout = TimeOut;
            sd.SelectCommand = cmd;
            sd.Fill(ds);
        }
        //catch (Exception)
        //{
        //    cmd.Dispose();
        //    ds.Dispose();
        //}
        catch (Exception ex)
        {
            throw new ApplicationException();
        }
        finally
        {
            if (ds == null)
            {
                cmd.Dispose();
                cn.Close();
            }
        }
        return ds;
    }
    public static SqlDataReader ExecuteReader(string query, SqlParameter[] p, string ConName)
    {
        SqlConnection cn = new SqlConnection(SetConnection(ConName));
        SqlCommand cmd = new SqlCommand();
        SqlDataReader dr = default(SqlDataReader);
        try
        {
            //fill(cmd, p);
            cn.Open();
            cmd.CommandText = query;
            cmd.Parameters.AddRange(p);
            cmd.Connection = cn;
            dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }

        catch (Exception ex)
        {
            throw new ApplicationException();

        }
        finally
        {
            if (dr == null)
            {
                cmd.Dispose();
                cn.Close();
            }
        }
        return dr;
    }
    public static SqlDataReader ExecuteReader(string query, string ConName)
    {
        SqlConnection cn = new SqlConnection(SetConnection(ConName));
        SqlCommand cmd = new SqlCommand();
        SqlDataReader dr = default(SqlDataReader);
        try
        {
            cn.Open();
            cmd.CommandText = query;
            cmd.Connection = cn;
            dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }

        catch (Exception)
        {
            throw new ApplicationException();
        }
        finally
        {
            if (dr == null)
            {
                cmd.Dispose();
                cn.Close();
            }
        }
        return dr;
    }
    public static string ExecuteScaler(string query, SqlParameter[] p, string ConName)
    {
        SqlConnection cn = new SqlConnection(SetConnection(ConName));
        SqlCommand cmd = new SqlCommand();

        try
        {
            cmd.CommandText = query;
            cmd.Connection = cn;
            cmd.Parameters.AddRange(p);
            //fill(cmd, p);
            cn.Open();
            string ret = Convert.ToString(cmd.ExecuteScalar());
            if (ret == null)
            {
                ret = "";
            }
            return ret;
        }
        catch (Exception ex)
        {
            //return "false";
            throw new ApplicationException();
        }
        finally
        {
            cmd.Dispose();
            cn.Close();
        }
    }

    public static SqlDataReader CallStoredProcedure(string Spname, SqlParameter[] p, string ConName)
    {
        SqlConnection cn = new SqlConnection(SetConnection(ConName));
        SqlCommand cmd = new SqlCommand();
        SqlDataReader dr = null;
        try
        {

            //fill(cmd, p);
            cmd.Parameters.AddRange(p);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = Spname;
            cmd.Connection = cn;
            cn.Open();
            dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

        }
        catch (Exception ex)
        {
            throw new ApplicationException();
        }
        finally
        {
            if (dr == null)
            {
                cmd.Dispose();
                cn.Close();
            }
        }
        return dr;
    }

    public static string ExecuteScaler(string query, string ConName)
    {
        SqlConnection cn = new SqlConnection(SetConnection(ConName));
        SqlCommand cmd = new SqlCommand();

        try
        {
            cmd.CommandText = query;
            cmd.Connection = cn;
            cn.Open();
            string ret = Convert.ToString(cmd.ExecuteScalar());
            if (ret == null)
            {
                ret = "";
            }
            return ret;
        }
        catch (Exception ex)
        {
            //return "false";
            throw new ApplicationException();
        }
        finally
        {
            cmd.Dispose();
            cn.Close();
        }
    }

    public static int ExecuteNonQuery(string query, SqlParameter[] p, string ConName)
    {
        SqlConnection cn = new SqlConnection(SetConnection(ConName));
        SqlCommand cmd = new SqlCommand();
        try
        {
            cmd.CommandText = query;
            cmd.Connection = cn;
            cmd.Parameters.AddRange(p);
            //fill(cmd, p);
            cn.Open();
            int ret = cmd.ExecuteNonQuery();
            return ret;
        }
        catch (Exception ex)
        {
            //return -1;
            throw new ApplicationException();
        }
        finally
        {
            cn.Close();
            cmd.Dispose();
        }
    }

    public static SqlDataReader CallStoredProcedureWithExecutionTime(string Spname, SqlParameter[] p, string ConName, int TimeOut)
    {
        SqlConnection cn = new SqlConnection(SetConnection(ConName));
        SqlCommand cmd = new SqlCommand();
        SqlDataReader dr = null;
        try
        {

            //fill(cmd, p);
            cmd.Parameters.AddRange(p);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = Spname;
            cmd.Connection = cn;
            cmd.CommandTimeout = TimeOut;
            cn.Open();
            dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

        }
        catch (Exception ex)
        {
            throw new ApplicationException();
        }
        finally
        {
            if (dr == null)
            {
                cmd.Dispose();
                cn.Close();
            }
        }
        return dr;
    }
    public static SqlDataReader ExecuteReaderWithTime(string query, string ConName, int TimeOut)
    {
        SqlConnection cn = new SqlConnection(SetConnection(ConName));
        SqlCommand cmd = new SqlCommand();
        SqlDataReader dr = default(SqlDataReader);
        try
        {
            cn.Open();
            cmd.CommandText = query;
            cmd.Connection = cn;
            cmd.CommandTimeout = TimeOut;
            dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }

        catch (Exception)
        {
            throw new ApplicationException();
        }
        finally
        {
            if (dr == null)
            {
                cmd.Dispose();
                cn.Close();
            }
        }
        return dr;
    }

    public static SqlDataReader ExecuteReaderWithTime(string query, SqlParameter[] p, string ConName, int TimeOut)
    {
        SqlConnection cn = new SqlConnection(SetConnection(ConName));
        SqlCommand cmd = new SqlCommand();
        SqlDataReader dr = default(SqlDataReader);
        try
        {
            //fill(cmd, p);
            cn.Open();
            cmd.CommandText = query;
            cmd.Parameters.AddRange(p);
            cmd.Connection = cn;
            cmd.CommandTimeout = TimeOut;
            dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }

        catch (Exception ex)
        {
            throw new ApplicationException();

        }
        finally
        {
            if (dr == null)
            {
                cmd.Dispose();
                cn.Close();
            }
        }
        return dr;
    }

}
