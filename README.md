---
categories: []
tags: [".net"] 
title: "Getting Started (.NET)"
linkTitle: "Getting Started (.NET)"
weight: 2
---

<!--

 Licensed to the Apache Software Foundation (ASF) under one
 or more contributor license agreements.  See the NOTICE file
 distributed with this work for additional information
 regarding copyright ownership.  The ASF licenses this file
 to you under the Apache License, Version 2.0 (the
 "License"); you may not use this file except in compliance
 with the License.  You may obtain a copy of the License at

   https://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing,
 software distributed under the License is distributed on an
 "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 KIND, either express or implied.  See the License for the
 specific language governing permissions and limitations
 under the License.

-->

This is a short guide for getting started with Apache Avro™ using .NET. This guide only covers using Avro for data serialization; see Patrick Hunt's [Avro RPC Quick Start](https://github.com/phunt/avro-rpc-quickstart) for a good introduction to using Avro for RPC.

## Download

Avro implementations for C, C++, C#, Java, PHP, Python, and Ruby can be downloaded from the [Apache Avro™ Download]({{< relref "/project/download" >}}) page. This guide uses Avro {{< avro_version >}}, the latest version at the time of writing.

Alternatively, if you are using .NET CLI, add the following dependency to your project:

```shell
dotnet add package Apache.Avro --version {{< avro_version >}}
```

## Defining a schema

Avro schemas are defined using JSON or IDL (the latter requires an extra dependency). Schemas are composed of primitive types (null, boolean, int, long, float, double, bytes, and string) and complex types (record, enum, array, map, union, and fixed). You can learn more about Avro schemas and types from the specification, but for now let's start with a simple schema example, user.avsc:

```json
{"namespace": "example.avro",
 "type": "record",
 "name": "User",
 "fields": [
     {"name": "name", "type": "string"},
     {"name": "favorite_number",  "type": ["int", "null"]},
     {"name": "favorite_color", "type": ["string", "null"]}
 ]
}
```
      
This schema defines a record representing a hypothetical user. (Note that a schema file can only contain a single schema definition.) At minimum, a record definition must include its type ("type": "record"), a name ("name": "User"), and fields, in this case name, favorite_number, and favorite_color. We also define a namespace ("namespace": "example.avro"), which together with the name attribute defines the "full name" of the schema (example.avro.User in this case).

Fields are defined via an array of objects, each of which defines a name and type (other attributes are optional, see the record specification for more details). The type attribute of a field is another schema object, which can be either a primitive or complex type. For example, the name field of our User schema is the primitive type string, whereas the favorite_number and favorite_color fields are both unions, represented by JSON arrays. unions are a complex type that can be any of the types listed in the array; e.g., favorite_number can either be an int or null, essentially making it an optional field.

## Serializing and deserializing with code generation

### Compiling the schema

Code generation allows us to automatically create classes based on our previously-defined schema. Once we have defined the relevant classes, there is no need to use the schema directly in our programs. We use the Apache.Avro.Tools .NET tools to generate code as follows:

```shell
dotnet tool install --global Apache.Avro.Tools --version 1.11.3
```

The `avrogen` (version: 1.11.3) requires .NET 7.0, you might also need to install the missings framework (e.g. on Windows):

```shell
winget install Microsoft.DotNet.Runtime.7
```

This will generate the appropriate source files based on the schema's namespace in the provided destination folder. For instance, to generate a User class from the schema defined above, run

```shell
avrogen -s User.avsc .
```

### Creating Users

Now that we've completed the code generation, let's create some Users, serialize them to a data file on disk, and then read back the file and deserialize the User objects.

First let's create some Users and set their fields.

```cs
// Leave favorite color null
User user1 = new User
{
    name = "Alyssa",
    favorite_number = 256
};

User user2 = new User
{
    name = "Ben",
    favorite_number = 7,
    favorite_color = "red"
};

// Leave favorite number color null
User user3 = new User
{
    name = "Charlie",
    favorite_color = "blue"
};
```

Note that we do not set user1's favorite color. Since that record is of type ["string", "null"], we can either set it to a string or leave it null; it is essentially optional. Similarly, we set user3's favorite number to null.

### Serializing
Now let's serialize our Users to disk.

```cs
// Serialize user1, user2 and user3 to disk
DatumWriter<User> userDatumWriter = new SpecificDatumWriter<User>(User._SCHEMA);
using (IFileWriter<User> dataFileWriter = DataFileWriter<User>.OpenWriter(userDatumWriter, "users.avro"))
{
    dataFileWriter.Append(user1);
    dataFileWriter.Append(user2);
    dataFileWriter.Append(user3);
}
```

We create a DatumWriter, which converts .NET objects into an in-memory serialized format. The SpecificDatumWriter class is used with generated classes and extracts the schema from the specified generated type.

Next we create a DataFileWriter, which writes the serialized records, as well as the schema, to the file specified in the DataFileWriter.OpenWriter call. We write our users to the file via calls to the dataFileWriter.Append method. When we are done writing, we close the data file.

### Deserializing

Finally, let's deserialize the data file we just created.

```cs
// Deserialize Users from disk
using (IFileReader<User> dataFileReader = DataFileReader<User>.OpenReader("users.avro"))
{
    User user = null!;
    while (dataFileReader.HasNext())
    {
        // Reuse user object by passing it to next(). This saves us from
        // allocating and garbage collecting many objects for files with
        // many items.
        user = dataFileReader.Next();
        Console.WriteLine($"{{\"name\": \"{user.name}\", "
            + $"\"favorite_number\": {(user.favorite_number == null ? "null" : "\"" + user.favorite_number + "\"")}, "
            + $"\"favorite_color\": {(user.favorite_color == null ? "null" : "\"" + user.favorite_color + "\"")}}}");
    }
}
```
        
This snippet will output:

```json
{"name": "Alyssa", "favorite_number": 256, "favorite_color": null}
{"name": "Ben", "favorite_number": 7, "favorite_color": "red"}
{"name": "Charlie", "favorite_number": null, "favorite_color": "blue"}
```

Deserializing is very similar to serializing. We create a DataFileReader, analogous to the DataFileWriter, which reads both the schema used by the writer as well as the data from the file on disk. The data will be read using the writer's schema included in the file and the schema provided by the reader, in this case the User class. The writer's schema is needed to know the order in which fields were written, while the reader's schema is needed to know what fields are expected and how to fill in default values for fields added since the file was written. If there are differences between the two schemas, they are resolved according to the Schema Resolution specification.

Next we use the DataFileReader to iterate through the serialized Users and print the deserialized object to stdout. Note how we perform the iteration: we create a single User object which we store the current deserialized user in, and pass this record object to every call of dataFileReader.HasNext. This is a performance optimization that allows the DataFileReader to reuse the same User object rather than allocating a new User for every iteration, which can be very expensive in terms of object allocation and garbage collection if we deserialize a large data file.

## Serializing and deserializing without code generation

Data in Avro is always stored with its corresponding schema, meaning we can always read a serialized item regardless of whether we know the schema ahead of time. This allows us to perform serialization and deserialization without code generation.

Let's go over the same example as in the previous section, but without using code generation: we'll create some users, serialize them to a data file on disk, and then read back the file and deserialize the users objects.

### Creating users

First, we use a SchemaParser to read our schema definition and create a Schema object.

```cs
Schema schema = Schema.Parse(File.ReadAllText("user.avsc"));
```

Using this schema, let's create some users.

```cs
GenericRecord user1 = new GenericData.Record(schema);
user1.put("name", "Alyssa");
user1.put("favorite_number", 256);
// Leave favorite color null

GenericRecord user2 = new GenericData.Record(schema);
user2.put("name", "Ben");
user2.put("favorite_number", 7);
user2.put("favorite_color", "red");
```

Since we're not using code generation, we use GenericRecords to represent users. GenericRecord uses the schema to verify that we only specify valid fields. If we try to set a non-existent field (e.g., user1.put("favorite_animal", "cat")), we'll get an AvroRuntimeException when we run the program.

Note that we do not set user1's favorite color. Since that record is of type ["string", "null"], we can either set it to a string or leave it null; it is essentially optional.

### Serializing

Now that we've created our user objects, serializing and deserializing them is almost identical to the example above which uses code generation. The main difference is that we use generic instead of specific readers and writers.

First we'll serialize our users to a data file on disk.

```cs
// Serialize user1 and user2 to disk
DatumWriter<GenericRecord> datumWriter = new GenericDatumWriter<GenericRecord>(schema);
using (IFileWriter<GenericRecord> dataFileWriter = DataFileWriter<GenericRecord>.OpenWriter(datumWriter, "users.avro"))
{
    dataFileWriter.Append(user1);
    dataFileWriter.Append(user2);
}
```

We create a DatumWriter, which converts .NET objects into an in-memory serialized format. Since we are not using code generation, we create a GenericDatumWriter. It requires the schema both to determine how to write the GenericRecords and to verify that all non-nullable fields are present.

As in the code generation example, we also create a DataFileWriter, which writes the serialized records, as well as the schema, to the file specified in the DataFileWriter.OpenWriter call. We write our users to the file via calls to the dataFileWriter.Append method. When we are done writing, we close the data file.

### Deserializing

Finally, we'll deserialize the data file we just created.

```cs
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
```

This outputs:

```json
{"name": "Alyssa", "favorite_number": 256, "favorite_color": null}
{"name": "Ben", "favorite_number": 7, "favorite_color": "red"}
```
        
Deserializing is very similar to serializing. We create a DataFileReader, analogous to the DataFileWriter, which reads both the schema used by the writer as well as the data from the file on disk. The data will be read using the writer's schema included in the file, and the reader's schema provided to the DataFileReader. The writer's schema is needed to know the order in which fields were written, while the reader's schema is needed to know what fields are expected and how to fill in default values for fields added since the file was written. If there are differences between the two schemas, they are resolved according to the Schema Resolution specification.

Next, we use the DataFileReader to iterate through the serialized users and print the deserialized object to stdout. Note how we perform the iteration: we create a single GenericRecord object which we store the current deserialized user in, and pass this record object to every call of dataFileReader.HasNext. This is a performance optimization that allows the DataFileReader to reuse the same record object rather than allocating a new GenericRecord for every iteration, which can be very expensive in terms of object allocation and garbage collection if we deserialize a large data file.