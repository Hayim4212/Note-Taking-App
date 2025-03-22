using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;

namespace notAlmaUygulamasi
{
    internal class Program
    {
        static int minId(string tableName, string tableId, SqlConnection connection)
        {
            string query = $@"
                DECLARE @newId INT;

                -- En küçük boş ID'yi bul
                SELECT @newId = MIN(t1.{tableId}) + 1 
                FROM {tableName} t1 
                WHERE NOT EXISTS (SELECT 1 FROM {tableName} t2 WHERE t2.{tableId} = t1.{tableId} + 1);

                -- Eğer boşluk yoksa, en büyük ID + 1 kullan
                IF @newId IS NULL 
                    SELECT @newId = COALESCE(MAX({tableId}), 0) + 1 FROM {tableName};

                SELECT @newId;";

            using (SqlCommand findMinId = new SqlCommand(query, connection))
            {
                object result = findMinId.ExecuteScalar();
                return result != DBNull.Value ? Convert.ToInt32(result) : 1;
            }
        }
        static void check(string tableName, SqlConnection connection)
        {
            // Tablo var mı kontrol et
            string checkTableQuery = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName";

            using (SqlCommand cmd = new SqlCommand(checkTableQuery, connection))
            {
                cmd.Parameters.AddWithValue("@TableName", tableName);
                int tableExists = (int)cmd.ExecuteScalar();

                if (tableExists == 0) // Eğer tablo yoksa oluştur
                {
                    string createTableQuery = "";

                    switch (tableName)
                    {
                        case "TblMainNotes":
                            createTableQuery = "CREATE TABLE TblMainNotes (NoteId INT PRIMARY KEY, NoteHeader NVARCHAR(50), NoteContent NVARCHAR(1000));";
                            break;
                        case "TblToDoList":
                            createTableQuery = "CREATE TABLE TblToDoList (ToDoId INT PRIMARY KEY, ToDo NVARCHAR(50));";
                            break;
                        case "TblCompletedToDoList":
                            createTableQuery = "CREATE TABLE TblCompletedToDoList (CompletedId INT PRIMARY KEY IDENTITY(1,1), CompletedToDo NVARCHAR(50));";
                            break;
                        default:
                            Console.WriteLine("Bilinmeyen tablo adı!");
                            return;
                    }

                    using (SqlCommand createCmd = new SqlCommand(createTableQuery, connection))
                    {
                        createCmd.ExecuteNonQuery();
                        Console.WriteLine($"{tableName} tablosu oluşturuldu.");
                    }
                }
                else
                {
                }
            }
        }
        static void Main(string[] args)
        {
            string addContent = "", addHeader = "", addTo_Do;
            int secim, dltNote, completeId;
            char basla = ' ';
            string connectionString = "Data Source=<Source>;initial Catalog=<DataBaseName>;integrated security=true";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                Console.Write("Baslamak Icin B veya b Yaziniz: ");
                basla = char.Parse(Console.ReadLine());
                do
                {
                    SqlCommand listNote = new SqlCommand("select*from TblMainNotes", conn);
                    SqlCommand listToDo = new SqlCommand("select*from TblToDoList", conn);
                    check("TblMainNotes", conn);
                    check("TblToDoList", conn);
                    check("TblCompletedToDoList", conn);
                    Console.WriteLine();
                    Console.WriteLine("Mevcut Notlar");
                    Console.WriteLine("------------------------------");
                    SqlDataReader readerNote = listNote.ExecuteReader();
                    while (readerNote.Read())
                    {
                        int id = readerNote.GetInt32(0);
                        string notBaslik = readerNote.GetString(1);
                        string notMetni = readerNote.GetString(2);

                        Console.WriteLine($"ID: {id} \n Not Basligi: {notBaslik} \n Not Icergi: {notMetni}");
                        Console.WriteLine();
                    }
                    readerNote.Close();
                    Console.WriteLine("------------------------------");
                    Console.WriteLine();
                    Console.WriteLine("Yapilacaklar");
                    Console.WriteLine("------------------------------");
                    SqlDataReader readerToDo = listToDo.ExecuteReader();
                    while (readerToDo.Read())
                    {
                        int id = readerToDo.GetInt32(0);
                        string notMetni = readerToDo.GetString(1);

                        Console.WriteLine($"ID: {id} \n Yapilacak: {notMetni}");
                        Console.WriteLine();
                    }
                    readerToDo.Close();
                    Console.WriteLine("------------------------------");
                    Console.WriteLine();
                    Console.WriteLine("------------------------------");
                    Console.Write("1-Not Ekleme\n2-Not Silme\n3-Yapilacaklar Ekleme\n4-Tamamlanmis Olarak Isaretleme\n5-Tamamlanmislari Goster\nYapmak Istediginiz Islemi Giriniz: ");
                    secim = int.Parse(Console.ReadLine());
                    Console.WriteLine("------------------------------");
                    switch (secim)
                    {
                        case 1:
                            SqlCommand addNote = new SqlCommand("insert into TblMainNotes values (@p1,@p2,@p3)", conn);
                            Console.Write("Baslik Giriniz(50 Karakter Max): ");
                            addHeader = Console.ReadLine();
                            Console.Write("Ekleme Yapiniz(1000 Karakter Max): ");
                            addContent = Console.ReadLine();
                            int newIdNote = minId("TblMainNotes","NoteId",conn);
                            addNote.Parameters.AddWithValue("@p1", newIdNote);
                            addNote.Parameters.AddWithValue("@p2", addHeader);
                            addNote.Parameters.AddWithValue("@p3", addContent);
                            addNote.ExecuteNonQuery();
                            Console.WriteLine();
                            Console.WriteLine("------------------------------");
                            Console.WriteLine("Not Basari Ile Eklendi");
                            Console.WriteLine("------------------------------");
                            break;
                        case 2:
                            SqlCommand deleteNote = new SqlCommand("Delete From TblMainNotes where NoteId=@NoteId", conn);
                            Console.Write("Silmek Istedigniz Notu Seciniz: ");
                            dltNote = int.Parse(Console.ReadLine());
                            deleteNote.Parameters.AddWithValue("@NoteId", dltNote);
                            deleteNote.ExecuteNonQuery();
                            Console.WriteLine();
                            Console.WriteLine("------------------------------");
                            Console.WriteLine("Not Basari Ile Silindi");
                            Console.WriteLine("------------------------------");

                            break;
                        case 3:
                            SqlCommand addToDo = new SqlCommand("insert into TblToDoList values (@p1,@p2)", conn);
                            Console.Write("Yapilacak Ekleyin(50 Karakter Max): ");
                            addTo_Do = Console.ReadLine();
                            int newIdToDo = minId("TblToDoList", "ToDoId", conn);
                            addToDo.Parameters.AddWithValue("@p1", newIdToDo);
                            addToDo.Parameters.AddWithValue("@p2", addTo_Do);
                            addToDo.ExecuteNonQuery();
                            Console.WriteLine();
                            Console.WriteLine("------------------------------");
                            Console.WriteLine("Yapilacak Basari Ile Eklendi");
                            Console.WriteLine("------------------------------");
                            break;
                        case 4:
                            SqlCommand completed = new SqlCommand(@"
                            insert into TblCompletedToDoList (CompletedToDo)
                            select ToDo from TblToDoList where ToDoId=@ToDoId
                            ", conn);
                            SqlCommand deleteToDo = new SqlCommand("delete from TblToDoList where ToDoId=@ToDoId", conn);
                            Console.Write("Tamamlandi Olarak Isaretlemek Istediginiz Yapilacagi Seciniz: ");
                            completeId = int.Parse(Console.ReadLine());
                            completed.Parameters.AddWithValue("@ToDoId", completeId);
                            deleteToDo.Parameters.AddWithValue("@ToDoId", completeId);
                            completed.ExecuteNonQuery();
                            deleteToDo.ExecuteNonQuery();
                            Console.WriteLine();
                            Console.WriteLine("------------------------------");
                            Console.WriteLine("Basari ile Isaretlendi");
                            Console.WriteLine("------------------------------");
                            break;
                        case 5:
                            SqlCommand listCompleted = new SqlCommand("select*from TblCompletedToDoList", conn);
                            SqlDataReader readerCompleted = listCompleted.ExecuteReader();
                            Console.WriteLine("Tamamlanmis Yapilacaklar");
                            Console.WriteLine("------------------------------");
                            while (readerCompleted.Read())
                            {
                                int id = readerCompleted.GetInt32(0);
                                string notMetni = readerCompleted.GetString(1);

                                Console.WriteLine($"ID: {id} \n Yapilacak: {notMetni}");
                                Console.WriteLine();
                            }
                            readerCompleted.Close();
                            Console.WriteLine("------------------------------");
                            break;
                        default:
                            Console.WriteLine();
                            Console.WriteLine("------------------------------");
                            Console.WriteLine("Yanlis Islem Girdiniz");
                            Console.WriteLine("------------------------------");

                            break;

                    }
                } while (basla == 'b' || basla == 'B');
            }
            Console.ReadLine();
        }
    }
}
