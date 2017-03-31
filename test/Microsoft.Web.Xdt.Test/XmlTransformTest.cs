using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Xunit;
using System.Reflection;
using Microsoft.Web.Xdt.Test.Properties;

namespace Microsoft.Web.XmlTransform.Test
{
    public class XmlTransformTest
    {

        [Fact]
        public void XmlTransform_Support_WriteToStream()
        {
            string src = CreateATestFile("Web.config", Resources.Web);
            string transformFile = CreateATestFile("Web.Release.config", Resources.Web_Release);
            string destFile = GetTestFilePath("MyWeb.config");

            //execute
            Microsoft.Web.XmlTransform.XmlTransformableDocument x = new Microsoft.Web.XmlTransform.XmlTransformableDocument();
            x.PreserveWhitespace = true;
            x.Load(src);

            Microsoft.Web.XmlTransform.XmlTransformation transform = new Microsoft.Web.XmlTransform.XmlTransformation(transformFile);

            bool succeed = transform.Apply(x);

            FileStream fsDestFile = new FileStream(destFile, FileMode.OpenOrCreate);
            x.Save(fsDestFile);

            //verify, we have a success transform
            Assert.Equal(true, succeed);

            //verify, the stream is not closed
            Assert.Equal(true, fsDestFile.CanWrite);

            //sanity verify the content is right, (xml was transformed)
            fsDestFile.Close();
            string content = File.ReadAllText(destFile);
            Assert.False(content.Contains("debug=\"true\""));
            
            List<string> lines = new List<string>(File.ReadLines(destFile));
            //sanity verify the line format is not lost (otherwsie we will have only one long line)
            Assert.True(lines.Count>10);

            //be nice 
            transform.Dispose();
            x.Dispose();
        }

        [Fact]
        public void XmlTransform_AttibuteFormatting()
        {
            Transform_TestRunner_ExpectSuccess(Resources.AttributeFormating_source,
                    Resources.AttributeFormating_transform,
                    Resources.AttributeFormating_destination,
                    Resources.AttributeFormatting_log);
        }

        [Fact]
        public void XmlTransform_TagFormatting()
        {
             Transform_TestRunner_ExpectSuccess(Resources.TagFormatting_source,
                    Resources.TagFormatting_transform,
                    Resources.TagFormatting_destination,
                    Resources.TagFormatting_log);
        }

        [Fact]
        public void XmlTransform_HandleEdgeCase()
        {
            //2 edge cases we didn't handle well and then fixed it per customer feedback.
            //    a. '>' in the attribute value
            //    b. element with only one character such as <p>
            Transform_TestRunner_ExpectSuccess(Resources.EdgeCase_source,
                    Resources.EdgeCase_transform,
                    Resources.EdgeCase_destination,
                    Resources.EdgeCase_log);
        }

        [Fact]
        public void XmlTransform_ErrorAndWarning()
        {
            Transform_TestRunner_ExpectFail(Resources.WarningsAndErrors_source,
                    Resources.WarningsAndErrors_transform,
                    Resources.WarningsAndErrors_log);
        }

        private void Transform_TestRunner_ExpectSuccess(string source, string transform, string baseline, string expectedLog)
        {
            string src = CreateATestFile("source.config", source);
            string transformFile = CreateATestFile("transform.config", transform);
            string baselineFile = CreateATestFile("baseline.config", baseline);
            string destFile = GetTestFilePath("result.config");
            TestTransformationLogger logger = new TestTransformationLogger();

            XmlTransformableDocument x = new XmlTransformableDocument();
            x.PreserveWhitespace = true;
            x.Load(src);

            Microsoft.Web.XmlTransform.XmlTransformation xmlTransform = new Microsoft.Web.XmlTransform.XmlTransformation(transformFile, logger);

            //execute
            bool succeed = xmlTransform.Apply(x);
            x.Save(destFile);
            xmlTransform.Dispose();
            x.Dispose();
            //test
            Assert.Equal(true, succeed);
            CompareFiles(destFile, baselineFile);
            CompareMultiLines(expectedLog, logger.LogText);
        }

        private void Transform_TestRunner_ExpectFail(string source, string transform, string expectedLog)
        {
            string src = CreateATestFile("source.config", source);
            string transformFile = CreateATestFile("transform.config", transform);
            string destFile = GetTestFilePath("result.config");
            TestTransformationLogger logger = new TestTransformationLogger();

            XmlTransformableDocument x = new XmlTransformableDocument();
            x.PreserveWhitespace = true;
            x.Load(src);

            Microsoft.Web.XmlTransform.XmlTransformation xmlTransform = new Microsoft.Web.XmlTransform.XmlTransformation(transformFile, logger);

            //execute
            bool succeed = xmlTransform.Apply(x);
            x.Save(destFile);
            xmlTransform.Dispose();
            x.Dispose();
            //test
            Assert.Equal(false, succeed);
            CompareMultiLines(expectedLog, logger.LogText);
        }

        private void CompareFiles(string baseLinePath, string resultPath)
        {
            string bsl;
            using (StreamReader sr = new StreamReader(baseLinePath))
            {
                bsl = sr.ReadToEnd();
            }

            string result;
            using (StreamReader sr = new StreamReader(resultPath))
            {
                result = sr.ReadToEnd();
            }

            CompareMultiLines(bsl, result);
        }

        private void CompareMultiLines(string baseline, string result)
        {
            string[] baseLines = baseline.Split(new string[] { System.Environment.NewLine },  StringSplitOptions.None);
            string[] resultLines = result.Split(new string[] { System.Environment.NewLine },  StringSplitOptions.None);

            for (int i = 0; i < baseLines.Length; i++)
            {
                Assert.Equal(baseLines[i], resultLines[i]);
            }
        }

        private string CreateATestFile(string filename, string contents)
        {
            string file = GetTestFilePath(filename);
            File.WriteAllText(file, contents);
            return file;
        }

        private string GetTestFilePath(string filename)
        {
            Uri asm = new Uri(typeof(XmlTransformTest).GetTypeInfo().Assembly.CodeBase, UriKind.Absolute);
            string dir = Path.GetDirectoryName(asm.LocalPath);
            string folder = Path.Combine(dir, "testfiles");
            Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, filename);
            return file;
        }
    }
}
