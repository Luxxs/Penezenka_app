using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Penezenka_App.Database;
using Penezenka_App.Model;

namespace Penezenka_App.OtherClasses
{
    [DataContract]
    class ExportData
    {
        [DataMember]
        public List<Account> Accounts { get; set; }
        [DataMember]
        public List<RecurrenceChain> RecurrenceChains { get; set; }
        [DataMember]
        public List<TagForExport> Tags { get; set; }
        [DataMember]
        public List<RecordForExport> Records { get; set; }
        [DataMember]
        public List<RecordTagForExport> RecordsTags { get; set; }
    }
    [DataContract]
    class RecordForExport
    {
        [DataMember]
        public int ID { get; set; }
        [DataMember]
        public int Date { get; set; }
        [DataMember]
        public string Title { get; set; }
        [DataMember]
        public double Amount { get; set; }
        [DataMember]
        public string Notes { get; set; }
        [DataMember]
        public int AccountID { get; set; }
        [DataMember]
        public int RecurrenceChainID { get; set; }
        [DataMember]
        public bool Automatically { get; set; }
    }
    [DataContract]
    class TagForExport
    {
        [DataMember]
        public int ID { get; set; }
        [DataMember]
        public string Title { get; set; }
        [DataMember]
        public uint Color { get; set; }
        [DataMember]
        public string Notes { get; set; }
    }
    [DataContract]
    class RecordTagForExport
    {
        [DataMember]
        public int Record_ID { get; set; }
        [DataMember]
        public int Tag_ID { get; set; }
    }

    static class Export
    {
        public static async void SaveAllDataToJson(string path)
        {
            var exportData = DB.GetExportData();
            var storageFolder = KnownFolders.DocumentsLibrary; 
            var fileOutputStream = await storageFolder.OpenStreamForWriteAsync(path, CreationCollisionOption.ReplaceExisting);
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ExportData));
            ser.WriteObject(fileOutputStream, exportData);
            fileOutputStream.Flush();
            fileOutputStream.Dispose();
        }
        public static async Task<ExportData> GetAllDataFromJson(string path)
        {
            var storageFolder = KnownFolders.DocumentsLibrary; 
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ExportData));
            var fileOutputStream = await storageFolder.OpenStreamForReadAsync(path);
            var exportData = (ExportData) ser.ReadObject(fileOutputStream);
            fileOutputStream.Dispose();
            return exportData;
        }
        public static async Task<ExportData> GetAllDataFromJson(IStorageFile file)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ExportData));
            var fileOutputStream = await file.OpenStreamForReadAsync();
            var exportData = (ExportData) ser.ReadObject(fileOutputStream);
            fileOutputStream.Dispose();
            return exportData;
        }

        public static void SaveExportedDataToDatabase(ExportData exportData)
        {
            DB.SaveDataFromExport(exportData);
        }


        public static string SerializeObjectToJsonString(object obj, Type dataType)
        {
            var memoryStream = new MemoryStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(dataType);
            ser.WriteObject(memoryStream, obj);
            return Encoding.UTF8.GetString(memoryStream.ToArray(), 0, (int)memoryStream.Length);
        }

        public static object DeserializeObjectFromJsonString(string str, Type dataType)
        {
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(str ?? ""));
            DataContractJsonSerializer ser = new DataContractJsonSerializer(dataType);
            return ser.ReadObject(memoryStream);
        }
    }
}
