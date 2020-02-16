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

        public struct InnerTestMessageStruct2
        {
            public int testInt2;
            public string testString2;
        }

        public struct TestMessageType1
        {
            public int testInt;
            public string testString;
            public InnerTestMessageStruct testStruct;
        }

        public struct TestMessageType2
        {
            public int testInt2;
            public string testString2;
            public InnerTestMessageStruct2 testStruct2;
        }

        public static TestMessageType1 TestMessageType1Sample1() {
            return new TestMessageType1() {
                testInt = 1,
                testString = "testMessageString",
                testStruct = new InnerTestMessageStruct() {
                    testInt = 11,
                    testString = "testInternalMessageStructTest"
                }
            };
        }

        public static TestMessageType2 TestMessageType2Sample1() {
            return new TestMessageType2() {
                testInt2 = 2,
                testString2 = "testMessageString2",
                testStruct2 = new InnerTestMessageStruct2() {
                    testInt2 = 22,
                    testString2 = "testInternalMessageStructTest2"
                }
            };
        }
    }
}
