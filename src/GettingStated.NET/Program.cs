using Avro;
using Avro.File;
using Avro.Generic;

RecordSchema schema = (RecordSchema)Schema.Parse(File.ReadAllText("user.avsc"));

GenericRecord user1 = new GenericRecord(schema);
user1.Add("name", "Alyssa");
user1.Add("favorite_number", 256);
// Leave favorite color null

GenericRecord user2 = new GenericRecord(schema);
user2.Add("name", "Ben");
user2.Add("favorite_number", 7);
user2.Add("favorite_color", "red");

// Serialize user1 and user2 to disk
DatumWriter<GenericRecord> datumWriter = new GenericDatumWriter<GenericRecord>(schema);
using (IFileWriter<GenericRecord> dataFileWriter = DataFileWriter<GenericRecord>.OpenWriter(datumWriter, "users.avro"))
{
    dataFileWriter.Append(user1);
    dataFileWriter.Append(user2);
}

// Deserialize users from disk
IFileReader<GenericRecord> dataFileReader = DataFileReader<GenericRecord>.OpenReader("users.avro");
GenericRecord user = null!;
while (dataFileReader.HasNext())
{
    // Reuse user object by passing it to next(). This saves us from
    // allocating and garbage collecting many objects for files with
    // many items.
    user = dataFileReader.Next();
    Console.WriteLine($"{{\"name\": \"{user["name"]}\", "
            + $"\"favorite_number\": {(user["favorite_number"] == null ? "null" : "\"" + user["favorite_number"] + "\"")}, "
            + $"\"favorite_color\": {(user["favorite_color"] == null ? "null" : "\"" + user["favorite_color"] + "\"")}}}");
}