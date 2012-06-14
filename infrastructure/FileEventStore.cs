using System;
using System.Collections.Generic;
using System.IO;

namespace lokad_iddd_sample
{
    /// <summary>
    /// TODO: import proper append-only event store from Lokad, when
    /// it is ready
    /// </summary>
    public sealed class FileEventStore : IEventStore
    {
        readonly IEventStoreStrategy _strategy;
        readonly string _folder;

        public FileEventStore(IEventStoreStrategy strategy, string folder)
        {
            _strategy = strategy;
            _folder = folder;
        }

        public EventStream LoadEventStream(IIdentity id, int skip, int take)
        {
            var fileName = _strategy.IdentityToString(id);
            var path = Path.Combine(_folder, fileName);

            var stream = new EventStream();
            if (!File.Exists(path))
                return stream;

            int position = 0;
            
            using(var file = File.OpenRead(path))
            {
                while (file.Position < file.Length)
                {
                    var size = ReadInt32(file);
                    var data = ReadBytes(file, size);

                    position += 1;

                    if ((position > skip) && (position <= skip + take))
                    {
                        stream.Events.Add(_strategy.DeserializeEvent(data));
                    }
                    
                    stream.Version += 1;
                }
            }
            return stream;
        }

        static byte[] ReadBytes(FileStream file, int size)
        {
            var data1 = new byte[size];
            file.Read(data1, 0, size);
            return data1;
        }

        static int ReadInt32(FileStream file)
        {
            var sizeBuffer = new byte[4];
            file.Read(sizeBuffer, 0, 4);
            var size = BitConverter.ToInt32(sizeBuffer, 0);
            return size;
        }
        public void AppendToStream(IIdentity id, int expectedVersion, ICollection<IEvent> events)
        {
            var fileName = _strategy.IdentityToString(id);
            var path = Path.Combine(_folder, fileName);

            var version = 0;
            using (var file = File.Open(path,FileMode.OpenOrCreate)) 
            {
                while (file.Position < file.Length)
                {
                    // skip
                    var count = ReadInt32(file);
                    file.Seek(count, SeekOrigin.Current);
                    version += 1;
                }
                if (expectedVersion != version)
                {
                    throw EventStoreConcurrencyException.Create(version, expectedVersion, fileName);
                }
                foreach (var @event in events)
                {
                    var data = _strategy.SerializeEvent(@event);
                    file.Write(BitConverter.GetBytes(data.Length), 0, 4);
                    file.Write(data,0,data.Length);
                }
            }
        }

        public void Initialize()
        {
            if (!Directory.Exists(_folder))
            {
                Directory.CreateDirectory(_folder);
            }
        }
    }
}