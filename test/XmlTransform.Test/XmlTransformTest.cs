using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Xml;
using XmlTransform.Test.Properties;
using Xunit;

namespace Microsoft.Web.XmlTransform.Test
{
    public class XmlTransformTest
    {
        [Fact]
        public void XmlTransform_Support_WriteToStream()
        {
            string src = CreateATestFile("Web.config", "Web.config", nameof(XmlTransform_Support_WriteToStream));
            string transformFile = CreateATestFile("Web.Release.config", "Web.release.config", nameof(XmlTransform_Support_WriteToStream));
            string destFile = GetTestFilePath("MyWeb.config", nameof(XmlTransform_Support_WriteToStream));

            //execute
            XmlTransformableDocument x = new XmlTransformableDocument();
            x.PreserveWhitespace = true;
            x.Load(src);

            Microsoft.Web.XmlTransform.XmlTransformation transform = new Microsoft.Web.XmlTransform.XmlTransformation(transformFile);

            bool succeed = transform.Apply(x);

            FileStream fsDestFile = new FileStream(destFile, FileMode.OpenOrCreate);
            {
                x.Save(fsDestFile);
            }

            //verify, we have a success transform
            Assert.True(succeed);

            //verify, the stream is not closed
            Assert.True(fsDestFile.CanWrite, "The file stream can not be written. was it closed?");

            //sanity verify the content is right, (xml was transformed)
            fsDestFile.Dispose();
            string content = File.ReadAllText(destFile);
            Assert.False(content.Contains("debug=\"true\""));

            List<string> lines = new List<string>(File.ReadLines(destFile));
            //sanity verify the line format is not lost (otherwsie we will have only one long line)
            Assert.True(lines.Count>10);

            //be nice
            transform.Dispose();
            x.Dispose();
        }

        //[Fact]
        public void XmlTransform_AttibuteFormatting()
        {
            Transform_TestRunner_ExpectSuccess("AttributeFormatting_source.xml",
                    "AttributeFormatting_transform.xml",
                    "AttributeFormatting_destination.bsl",
                    "AttributeFormatting.log");
        }

        [Fact]
        public void XmlTransform_TagFormatting()
        {
             Transform_TestRunner_ExpectSuccess("TagFormatting_source.xml",
                    "TagFormatting_transform.xml",
                    "TagFormatting_destination.bsl",
                    "TagFormatting.log");
        }

        [Fact]
        public void XmlTransform_HandleEdgeCase()
        {
            //2 edge cases we didn't handle well and then fixed it per customer feedback.
            //    a. '>' in the attribute value
            //    b. element with only one character such as <p>
            Transform_TestRunner_ExpectSuccess("EdgeCase_source.xml",
                    "EdgeCase_transform.xml",
                    "EdgeCase_destination.bsl",
                    "EdgeCase.log");
        }

        [Fact]
        public void XmlTransform_ErrorAndWarning()
        {
            Transform_TestRunner_ExpectFail("WarningsAndErrors_source.xml",
                    "WarningsAndErrors_transform.xml",
                    "WarningsAndErrors.log");
        }

        private void Transform_TestRunner_ExpectSuccess(string source, string transform, string baseline, string expectedLog, [CallerMemberName]string testName = null)
        {
            string src = CreateATestFile("source.config", source, testName);
            string transformFile = CreateATestFile("transform.config", transform, testName);
            string baselineFile = CreateATestFile("baseline.config", baseline, testName);
            string destFile = GetTestFilePath("result.config", testName);
            TestTransformationLogger logger = new TestTransformationLogger();

            XmlTransformableDocument x = new XmlTransformableDocument();
            x.PreserveWhitespace = true;
            string text = File.ReadAllText(src);
            XmlReader reader = XmlReader.Create(new StringReader(text));
            x.Load(reader);

            XmlTransformation xmlTransform = new XmlTransformation(transformFile, logger);

            //execute
            bool succeed = xmlTransform.Apply(x);
            x.Save(destFile);

            xmlTransform.Dispose();
            x.Dispose();
            //test
            Assert.True(succeed);
            CompareFiles(baselineFile, destFile);
            string sourceFile = GetSourceFilePath(expectedLog);
            string sourceText = File.ReadAllText(sourceFile);
            CompareMultiLines(sourceText, logger.LogText);
        }

        private void Transform_TestRunner_ExpectFail(string source, string transform, string expectedLog, [CallerMemberName]string testName = null)
        {
            string src = CreateATestFile("source.config", source, testName);
            string transformFile = CreateATestFile("transform.config", transform, testName);
            string destFile = GetTestFilePath("result.config", testName);
            TestTransformationLogger logger = new TestTransformationLogger();

            XmlTransformableDocument x = new XmlTransformableDocument();
            x.PreserveWhitespace = true;
            x.Load(src);

            XmlTransformation xmlTransform = new XmlTransformation(transformFile, logger);

            //execute
            bool succeed = xmlTransform.Apply(x);
            x.Save(destFile);

            xmlTransform.Dispose();
            x.Dispose();
            //test
            Assert.False(succeed);
            string sourceFile = GetSourceFilePath(expectedLog);
            string sourceText = File.ReadAllText(sourceFile);
            CompareMultiLines(sourceText, logger.LogText);
        }

        private void CompareFiles(string baseLinePath, string resultPath)
        {
            string bsl;
            using (StreamReader sr = new StreamReader(File.OpenRead(baseLinePath)))
            {
                bsl = sr.ReadToEnd();
            }

            string result;
            using (StreamReader sr = new StreamReader(File.OpenRead(resultPath)))
            {
                result = sr.ReadToEnd();
            }

            CompareMultiLines(bsl, result);
        }

        private void CompareMultiLines(string baseline, string result)
        {
            //string[] baseLines = baseline.Split(new string[] { System.Environment.NewLine },  StringSplitOptions.None);
            //string[] resultLines = result.Split(new string[] { System.Environment.NewLine },  StringSplitOptions.None);

            //for (int i = 0; i < baseLines.Length; i++)
            //{
            //    Assert.Equal(baseLines[i], resultLines[i]);
            //}
            string baseLineString = RemoveWhitespace(baseline);
            string resultString = RemoveWhitespace(result);
            Assert.Equal(baseLineString, resultString);
        }

        public string RemoveWhitespace(string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }

        private string CreateATestFile(string filename, string sourceFile, string testName)
        {
            string source = GetSourceFilePath(sourceFile);
            string sourceText = File.ReadAllText(source);
            string file = GetTestFilePath(filename, testName);
            File.WriteAllText(file, sourceText);
            return file;
        }

        private string GetTestFilePath(string filename, string testName)
        {
            Uri asm = new Uri(typeof(XmlTransformTest).GetTypeInfo().Assembly.CodeBase, UriKind.Absolute);
            string dir = Path.GetDirectoryName(asm.LocalPath);
            string folder = Path.Combine(dir, testName);
            Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, filename);
            return file;
        }

        private string GetSourceFilePath(string filename)
        {
            Uri asm = new Uri(typeof(XmlTransformTest).GetTypeInfo().Assembly.CodeBase, UriKind.Absolute);
            string dir = Path.GetDirectoryName(asm.LocalPath);
            string folder = Path.Combine(dir, "Resources");
            string file = Path.Combine(folder, filename);
            return file;
        }
    }
}
