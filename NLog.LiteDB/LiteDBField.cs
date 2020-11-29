using System;
using System.ComponentModel;
using NLog.Config;
using NLog.Layouts;

namespace NLog.LiteDB
{
    /// <summary>
    /// A configuration item for LiteDB target.
    /// </summary>
    [NLogConfigurationItem]
    public sealed class LiteDBField
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LiteDBField"/> class.
        /// </summary>
        public LiteDBField() 
            : this(null, null, "String")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteDBField"/> class.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        /// <param name="layout">The layout used to generate the value for the field.</param>
        public LiteDBField(string name, Layout layout)
            : this(name, layout, "String")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteDBField" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="layout">The layout.</param>
        /// <param name="bsonType">The bson type.</param>
        public LiteDBField(string name, Layout layout, string bsonType)
        {
            Name = name;
            Layout = layout;
            BsonType = bsonType ?? "String";
        }

        /// <summary>
        /// Gets or sets the name of the LiteDB field.
        /// </summary>
        /// <value>
        /// The name of the MongoDB field.
        /// </value>
        [RequiredParameter]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the layout used to generate the value for the field.
        /// </summary>
        /// <value>
        /// The layout used to generate the value for the field.
        /// </value>
        [RequiredParameter]
        public Layout Layout { get; set; }

        /// <summary>
        /// Gets or sets the bson type of the field. Possible values are Boolean, DateTime, Double, Int32, Int64 and String
        /// </summary>
        /// <value>
        /// The bson type of the field..
        /// </value>
        [DefaultValue("String")]
        public string BsonType { get; set; }
    }
}