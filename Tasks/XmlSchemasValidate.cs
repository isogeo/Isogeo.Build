// $Id: XmlSchemasValidate.cs 5771 2010-09-10 12:30:31Z mcartoixa $
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using Microsoft.Win32;

namespace Isogeo.Build.Tasks
{

    /// <summary>Validates the specified schema files.</summary>
    /// <example>
    /// <code><![CDATA[
    /// <Target Name="Test">
    ///   <XmlSchemasValidate
    ///     Schemas="Test.xsd"
    ///   />
    /// </Target>
    /// ]]></code>
    /// </example>
    public class XmlSchemasValidate:
        Task
    {
        public XmlSchemasValidate()
        {
        }

        public override bool Execute()
        {
            _Success=true;
            var schemaSet=new XmlSchemaSet();

            foreach (ITaskItem ti in Schemas)
                try
                {
                    schemaSet.Add(XmlSchema.Read(File.OpenRead(Path.GetFullPath(ti.ItemSpec)), new ValidationEventHandler(_SchemaValidated)));
                } catch (XmlException xex)
                {
                    Log.LogError(
                        null,
                        null,
                        xex.HelpLink,
                        Path.GetFullPath(ti.ItemSpec),
                        xex.LineNumber,
                        xex.LinePosition,
                        0,
                        0,
                        xex.Message
                    );
                    _Success=false;
                }

            try
            {
                schemaSet.Compile();
            } catch (XmlSchemaException xsex)
            {
                Log.LogError(
                    null,
                    null,
                    xsex.HelpLink,
                    null,
                    xsex.LineNumber,
                    xsex.LinePosition,
                    0,
                    0,
                    xsex.Message
                );
                _Success=false;
            }

            return _Success;
        }

        private void _SchemaValidated(object sender, ValidationEventArgs e)
        {
            switch (e.Severity)
            {
            case XmlSeverityType.Warning:
                Log.LogWarning(
                    null,
                    null,
                    e.Exception.HelpLink,
                    null,
                    e.Exception.LineNumber,
                    e.Exception.LinePosition,
                    0,
                    0,
                    e.Exception.Message
                );
                break;
            case XmlSeverityType.Error:
                Log.LogError(
                    null,
                    null,
                    e.Exception.HelpLink,
                    null,
                    e.Exception.LineNumber,
                    e.Exception.LinePosition,
                    0,
                    0,
                    e.Exception.Message
                );
                _Success=false;
                break;
            default:
                Log.LogMessage(
                    null,
                    null,
                    e.Exception.HelpLink,
                    null,
                    e.Exception.LineNumber,
                    e.Exception.LinePosition,
                    0,
                    0,
                    e.Exception.Message
                );
                break;
            }
        }

        [Required]
        public ITaskItem[] Schemas
        {
            get;
            set;
        }

        private bool _Success;
    }
}
