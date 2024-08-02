using System;
using System.Collections.Generic;
using System.Linq;
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

namespace PCTSApp.Models
{
    public class ConnectDB
    {
        private static string ConCnaa = ConfigurationManager.ConnectionStrings["cnaaCon"].ToString();
        private static string Con_Rajmedical = ConfigurationManager.ConnectionStrings["RajmedicalCon"].ToString();

        public ConnectDB()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        public static string SetConnection(string ConName)
        {
            string ConnectionString = "";
            if (ConName.ToLower() == "rajmedical")
            {
                ConnectionString = Convert.ToString(Con_Rajmedical);
            }
            else
            {
                ConnectionString = Convert.ToString(ConCnaa);
            }
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


        public static DataSet DataSet(string query, SqlParameter[] p, string ConName)
        {
            DataSet ds = null;
            using (SqlConnection cn = new SqlConnection(SetConnection(ConName)))
            {
                ds = new DataSet();
                using (SqlCommand cmd = new SqlCommand())
                {
                    try
                    {
                        cmd.CommandText = query;
                        cmd.Connection = cn;
                        SqlDataAdapter sd = new SqlDataAdapter();
                        fill(cmd, p);
                        sd.SelectCommand = cmd;
                        sd.Fill(ds);
                    }
                    catch (Exception ex)
                    {
                        ds.Dispose();
                        throw new ApplicationException(ex.Message);
                    }
                }
            }
            return ds;
        }
        public static SqlDataReader ExecuteReader(string query, SqlParameter[] p, string ConName)
        {
            SqlDataReader dr = null;
            if (!string.IsNullOrEmpty(query))
            {
                SqlConnection cn = new SqlConnection(SetConnection(ConName));
                SqlCommand cmd = new SqlCommand();
                dr = default(SqlDataReader);
                try
                {
                    fill(cmd, p);
                    cn.Open();
                    cmd.CommandText = query;
                    cmd.Connection = cn;
                    dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                }

                catch (Exception ex)
                {
                    throw new ApplicationException(ex.Message);

                }
                finally
                {
                    if (dr == null)
                    {
                        cmd.Dispose();
                        cn.Close();
                    }
                }
            }
            return dr;
        }
        public static SqlDataReader ExecuteReader(string query, string ConName)
        {
            SqlDataReader dr = null;
            if (!string.IsNullOrEmpty(query))
            {
                SqlConnection cn = new SqlConnection(SetConnection(ConName));
                SqlCommand cmd = new SqlCommand();
                dr = default(SqlDataReader);
                try
                {
                    cn.Open();
                    cmd.CommandText = query;
                    cmd.Connection = cn;
                    dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                }

                catch (Exception ex)
                {
                    throw new ApplicationException(ex.Message);
                }
                finally
                {
                    if (dr == null)
                    {
                        cmd.Dispose();
                        cn.Close();
                    }
                }
            }
            return dr;
        }
        public static string ExecuteScaler(string query, SqlParameter[] p, string ConName)
        {
            if (!string.IsNullOrEmpty(query))
            {
                using (SqlConnection cn = new SqlConnection(SetConnection(ConName)))
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        try
                        {
                            cmd.CommandText = query;
                            cmd.Connection = cn;
                            cmd.Parameters.Clear();
                            fill(cmd, p);
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
                            throw new ApplicationException(ex.Message);
                        }
                        finally
                        {
                            cmd.Dispose();
                            cn.Close();
                        }
                    }
                }
            }
            else
            {
                throw new ApplicationException("Query is null or empty");
            }
        }
        public static string ExecuteScaler(string query, string ConName)
        {
            if (!string.IsNullOrEmpty(query))
            {
                using (SqlConnection cn = new SqlConnection(SetConnection(ConName)))
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
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
                            throw new ApplicationException(ex.Message);
                        }
                        finally
                        {
                            cmd.Dispose();
                            cn.Close();
                        }
                    }
                }
            }
            else
            {
                throw new ApplicationException("Query is null or empty");
            }
        }

        public static SqlDataReader CallStoredProcedure(string Spname, SqlParameter[] p, string ConName)
        {
            SqlDataReader dr = null;
            if (!string.IsNullOrEmpty(Spname))
            {
                SqlConnection cn = new SqlConnection(SetConnection(ConName));
                SqlCommand cmd = new SqlCommand();
                try
                {

                    fill(cmd, p);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = Spname;
                    cmd.Connection = cn;
                    cn.Open();
                    dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                }
                catch (Exception ex)
                {
                    throw new ApplicationException(ex.Message);
                }
                finally
                {
                    if (dr == null)
                    {
                        cmd.Dispose();
                        cn.Close();
                    }
                }
            }
            else
            {
                throw new ApplicationException("spname is null or empty");
            }
            return dr;
        }


        public static int ExecuteNonQuery(string query, string ConName)
        {
            if (!string.IsNullOrEmpty(query))
            {
                using (SqlConnection cn = new SqlConnection(SetConnection(ConName)))
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        try
                        {
                            cmd.CommandText = query;
                            cmd.Connection = cn;
                            cn.Open();
                            int ret = cmd.ExecuteNonQuery();
                            return ret;
                        }
                        catch (Exception ex)
                        {
                            throw new ApplicationException(ex.Message);
                        }
                        finally
                        {
                            cn.Close();
                            cmd.Dispose();
                        }
                    }
                }
            }
            else
            {
                throw new ApplicationException("Query is null or empty");
            }
        }
        public static int ExecuteNonQuery(string query, SqlParameter[] p, string ConName)
        {
            if (!string.IsNullOrEmpty(query))
            {
                using (SqlConnection cn = new SqlConnection(SetConnection(ConName)))
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        try
                        {
                            cmd.CommandText = query;
                            cmd.Connection = cn;
                            fill(cmd, p);
                            cn.Open();
                            int ret = cmd.ExecuteNonQuery();
                            return ret;
                        }
                        catch (Exception ex)
                        {
                            throw new ApplicationException(ex.Message);
                        }
                        finally
                        {
                            cn.Close();
                            cmd.Dispose();
                        }
                    }
                }
            }
            else
            {
                throw new ApplicationException("Query is null or empty");
            }
        }

        public static string ExecuteScaler(string Spname, SqlParameter[] p, string ConName, int notuse)
        {
            if (!string.IsNullOrEmpty(Spname))
            {
                using (SqlConnection cn = new SqlConnection(SetConnection(ConName)))
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        try
                        {

                            fill(cmd, p);
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.CommandText = Spname;
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
                            throw new ApplicationException(ex.Message);
                        }
                        finally
                        {
                            cmd.Dispose();
                            cn.Close();
                        }
                    }
                }
            }
            else
            {
                throw new ApplicationException("Query is null or empty");
            }
        }
        public static SqlDataReader ExecuteReader(string query, SqlParameter[] p, string ConName, int CommandTimeout)
        {
            SqlDataReader dr = null;
            if (!string.IsNullOrEmpty(query))
            {
                SqlConnection cn = new SqlConnection(SetConnection(ConName));
                SqlCommand cmd = new SqlCommand();
                dr = default(SqlDataReader);
                try
                {
                    fill(cmd, p);
                    cn.Open();
                    cmd.CommandText = query;
                    if (CommandTimeout > 0)
                    {
                        cmd.CommandTimeout = CommandTimeout;
                    }
                    cmd.Connection = cn;
                    dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                }

                catch (Exception ex)
                {
                    throw new ApplicationException(ex.Message);

                }
                finally
                {
                    if (dr == null)
                    {
                        cmd.Dispose();
                        cn.Close();
                    }
                }
            }
            return dr;
        }
        public static DataSet DataSet(string query, SqlParameter[] p, string ConName, int CommandTimeout)
        {
            DataSet ds = null;
            using (SqlConnection cn = new SqlConnection(SetConnection(ConName)))
            {
                ds = new DataSet();
                using (SqlCommand cmd = new SqlCommand())
                {
                    try
                    {
                        cmd.CommandText = query;
                        cmd.Connection = cn;
                        if (CommandTimeout > 0)
                        {
                            cmd.CommandTimeout = CommandTimeout;
                        }
                        SqlDataAdapter sd = new SqlDataAdapter();
                        fill(cmd, p);
                        sd.SelectCommand = cmd;
                        sd.Fill(ds);
                    }
                    catch (Exception ex)
                    {
                        ds.Dispose();
                        throw new ApplicationException(ex.Message);
                    }
                }
            }
            return ds;
        }
        public static SqlDataReader CallStoredProcedure(string Spname, SqlParameter[] p, string ConName, int CommandTimeout)
        {
            SqlDataReader dr = null;
            if (!string.IsNullOrEmpty(Spname))
            {
                SqlConnection cn = new SqlConnection(SetConnection(ConName));
                SqlCommand cmd = new SqlCommand();
                try
                {

                    fill(cmd, p);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = Spname;
                    cmd.Connection = cn;
                    if (CommandTimeout > 0)
                    {
                        cmd.CommandTimeout = CommandTimeout;
                    }
                    cn.Open();
                    dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                }
                catch (Exception ex)
                {
                    throw new ApplicationException(ex.Message);
                }
                finally
                {
                    if (dr == null)
                    {
                        cmd.Dispose();
                        cn.Close();
                    }
                }
            }
            else
            {
                throw new ApplicationException("spname is null or empty");
            }
            return dr;
        }
    }

}