using System;
using System.Collections.Generic;
using System.Text;

namespace PixieTests.Common
{
    internal class TestMessages
    {
        public struct InnerTestMessageStruct
        {
            public int testInt;
            public string testString;
        }

        public struct TestMessageType1
        {
            public int testInt;
            public string testString;
            public InnerTestMessageStruct testStruct;
        }

        public static TestMessageType1 TestMessageType1Sample1() {
            return new TestMessageType1() {
                testInt = 1,
                testString = "testMessageString",
                testStruct = new InnerTestMessageStruct() {
                    testInt = 2,
                    testString = "testInternalMessageStructTest"
                }
            };
        }
    }
}
