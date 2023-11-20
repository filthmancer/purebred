using System;
using System.Collections.Generic;
using IO = System.IO;
using FileIO = System.IO.File;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Tomlyn;
using Godot;
using System.IO;
using System.Linq;

public class File
{
    internal class FileData
    {
        public IO.FileInfo info;
        public object value;
    }
    internal static List<FileData> FileDataInstances = new List<FileData>();

    public const string DEV_PATH = "res://data/";
    public static string SaveDirectory => Godot.ProjectSettings.GlobalizePath(DEV_PATH);

    internal static bool FileDataLoaded(string _path, out FileData _fileData)
    {
        if (!_path.Contains(SaveDirectory))
        {
            _path = SaveDirectory + _path;
        }
        _fileData = FileDataInstances.Find(fdi =>
        {
            return fdi.info.FullName == _path;
        });
        if (_fileData != null)
            return true;

        _fileData = new FileData()
        {
            info = new IO.FileInfo(_path)
        };
        FileDataInstances.Add(_fileData);
        return false;
    }

    public static void Test()
    {
        Task.Run(async () =>
        {
            int[] arr = new int[3] { 0, 1, 2 };
            await SaveArray<int>(SaveDirectory + "test.txt", arr);
            await SaveArray<int>(SaveDirectory + "test.txt", arr);
        });

    }

    /// <summary>
    /// Asynch loads a string from this path
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static async Task<string> LoadText(string _path)
    {
        if (!FileDataLoaded(SaveDirectory + _path, out FileData fdi))
        {
            using (System.IO.StreamReader reader = FileIO.OpenText(fdi.info.FullName))
            {
                string text = await reader.ReadToEndAsync();
                reader.Close();
                fdi.value = text;
            }
        }
        return (string)fdi.value;
    }

    /// <summary>
    /// Async loads an object from this path
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_path"></param>
    /// <returns></returns>
    public static async Task<T> LoadJson<T>(string _path)
    {
        if (!FileDataLoaded(SaveDirectory + _path, out FileData fdi))
        {
            using (System.IO.StreamReader reader = FileIO.OpenText(fdi.info.FullName))
            {
                string text = await reader.ReadToEndAsync();
                reader.Close();

                T _value = JsonConvert.DeserializeObject<T>(text);
                fdi = new FileData()
                {
                    value = _value
                };
                FileDataInstances.Add(fdi);
            }
        }
        return (T)fdi.value;
    }

    public static async Task<T> LoadToml<T>(string _path) where T : class, new()
    {
        if (!FileDataLoaded(SaveDirectory + _path, out FileData fdi))
        {
            using (System.IO.StreamReader reader = FileIO.OpenText(fdi.info.FullName))
            {
                string text = await reader.ReadToEndAsync();
                reader.Close();
                var model = Toml.ToModel<T>(text);
                fdi.value = model;
            }
        }
        return (T)fdi.value;
    }

    public static async Task<T[]> LoadArray<T>(string _path) where T : IConvertible
    {
        if (!FileDataLoaded(_path, out FileData fdi))
        {
            using (System.IO.StreamReader reader = FileIO.OpenText(fdi.info.FullName))
            {
                string text = await reader.ReadToEndAsync();
                reader.Close();

                string[] values = text.Split(',');
                T[] values1 = new T[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    values1[i] = (T)Convert.ChangeType(values[i], typeof(T));
                }
                fdi.value = values1;
            }
        }
        return (T[])fdi.value;
    }

    public static async Task SaveJson(string _path, object _data)
    {
        if (!FileDataLoaded(_path, out FileData fdi))
        {
        }
        fdi.value = _data;

        string text = JsonConvert.SerializeObject(_data);
        await FileIO.WriteAllTextAsync(fdi.info.FullName, text);
    }

    public static async Task SaveArray<T>(string _path, T[] _data)
    {
        if (!FileDataLoaded(_path, out FileData fdi))
        {
        }
        fdi.value = _data;


        string text = "";
        for (int i = 0; i < _data.Length; i++)
        {
            text += (_data[i].ToString());
            if (i != _data.Length - 1)
                text += ",";
        }

        await FileIO.WriteAllTextAsync(fdi.info.FullName, text);
    }
}
