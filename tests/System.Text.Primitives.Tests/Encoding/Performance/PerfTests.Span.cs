﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;
using Microsoft.Xunit.Performance;
using System.Text.Primitives.Tests.Encoding;
using System.Buffers;

namespace System.Text.Primitives.Tests
{
    public partial class EncodingPerfComparisonTests
    {
        private const int InnerCount = 1000;

        private static IEnumerable<object[]> GetEncodingPerformanceTestData()
        {
            var data = new List<object[]>();
            data.Add(new object[] { 99, 0x0, TextEncoderConstants.Utf8OneByteLastCodePoint });
            data.Add(new object[] { 99, TextEncoderConstants.Utf8OneByteLastCodePoint + 1, TextEncoderConstants.Utf8TwoBytesLastCodePoint });
            data.Add(new object[] { 99, TextEncoderConstants.Utf8TwoBytesLastCodePoint + 1, TextEncoderConstants.Utf8ThreeBytesLastCodePoint });
            data.Add(new object[] { 99, TextEncoderConstants.Utf16HighSurrogateFirstCodePoint, TextEncoderConstants.Utf16LowSurrogateLastCodePoint });
            data.Add(new object[] { 99, 0x0, TextEncoderConstants.Utf8ThreeBytesLastCodePoint });
            data.Add(new object[] { 99, 0, 0, SpecialTestCases.AlternatingASCIIAndNonASCII });
            data.Add(new object[] { 99, 0, 0, SpecialTestCases.MostlyASCIIAndSomeNonASCII });
            data.Add(new object[] { 999, 0x0, TextEncoderConstants.Utf8OneByteLastCodePoint });
            data.Add(new object[] { 999, TextEncoderConstants.Utf8OneByteLastCodePoint + 1, TextEncoderConstants.Utf8TwoBytesLastCodePoint });
            data.Add(new object[] { 999, TextEncoderConstants.Utf8TwoBytesLastCodePoint + 1, TextEncoderConstants.Utf8ThreeBytesLastCodePoint });
            data.Add(new object[] { 999, TextEncoderConstants.Utf16HighSurrogateFirstCodePoint, TextEncoderConstants.Utf16LowSurrogateLastCodePoint });
            data.Add(new object[] { 999, 0x0, TextEncoderConstants.Utf8ThreeBytesLastCodePoint });
            data.Add(new object[] { 999, 0, 0, SpecialTestCases.AlternatingASCIIAndNonASCII });
            data.Add(new object[] { 999, 0, 0, SpecialTestCases.MostlyASCIIAndSomeNonASCII });
            data.Add(new object[] { 9999, 0x0, TextEncoderConstants.Utf8OneByteLastCodePoint });
            data.Add(new object[] { 9999, TextEncoderConstants.Utf8OneByteLastCodePoint + 1, TextEncoderConstants.Utf8TwoBytesLastCodePoint });
            data.Add(new object[] { 9999, TextEncoderConstants.Utf8TwoBytesLastCodePoint + 1, TextEncoderConstants.Utf8ThreeBytesLastCodePoint });
            data.Add(new object[] { 9999, TextEncoderConstants.Utf16HighSurrogateFirstCodePoint, TextEncoderConstants.Utf16LowSurrogateLastCodePoint });
            data.Add(new object[] { 9999, 0x0, TextEncoderConstants.Utf8ThreeBytesLastCodePoint });
            data.Add(new object[] { 9999, 0, 0, SpecialTestCases.AlternatingASCIIAndNonASCII });
            data.Add(new object[] { 9999, 0, 0, SpecialTestCases.MostlyASCIIAndSomeNonASCII });
            return data;
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [MemberData(nameof(GetEncodingPerformanceTestData))]
        public void EncodeFromUtf16toUtf8UsingTextEncoder(int length, int minCodePoint, int maxCodePoint, SpecialTestCases special = SpecialTestCases.None)
        {
            string inputString = GenerateStringData(length, minCodePoint, maxCodePoint, special);
            ReadOnlySpan<byte> utf16 = inputString.AsSpan().AsBytes();

            var status = Encoders.Utf8.ComputeEncodedBytesFromUtf16(utf16, out int needed);
            Assert.Equal(TransformationStatus.Done, status);

            Span<byte> utf8 = new byte[needed];

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        status = Encoders.Utf8.ConvertFromUtf16(utf16, utf8, out int consumed, out int written);
                        if (status != TransformationStatus.Done)
                            throw new Exception();
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [MemberData(nameof(GetEncodingPerformanceTestData))]
        public void EncodeFromUtf32toUtf8UsingTextEncoder(int length, int minCodePoint, int maxCodePoint, SpecialTestCases special = SpecialTestCases.None)
        {
            string inputString = GenerateStringData(length, minCodePoint, maxCodePoint, special);
            ReadOnlySpan<byte> utf32 = Text.Encoding.UTF32.GetBytes(inputString);

            var status = Encoders.Utf8.ComputeEncodedBytesFromUtf32(utf32, out int needed);
            Assert.Equal(TransformationStatus.Done, status);

            Span<byte> utf8 = new byte[needed];

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        status = Encoders.Utf8.ConvertFromUtf32(utf32, utf8, out int consumed, out int written);
                        if (status != TransformationStatus.Done)
                            throw new Exception();
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [MemberData(nameof(GetEncodingPerformanceTestData))]
        public void EncodeFromUtf8toUtf16UsingTextEncoder(int length, int minCodePoint, int maxCodePoint, SpecialTestCases special = SpecialTestCases.None)
        {
            string inputString = GenerateStringData(length, minCodePoint, maxCodePoint, special);
            ReadOnlySpan<byte> utf8 = Text.Encoding.UTF8.GetBytes(inputString);

            var status = Encoders.Utf16.ComputeEncodedBytesFromUtf8(utf8, out int needed);
            Assert.Equal(TransformationStatus.Done, status);

            Span<byte> utf16 = new byte[needed];

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        status = Encoders.Utf16.ConvertFromUtf8(utf8, utf16, out int consumed, out int written);
                        if (status != TransformationStatus.Done)
                            throw new Exception();
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [MemberData(nameof(GetEncodingPerformanceTestData))]
        public void EncodeFromUtf32toUtf16UsingTextEncoder(int length, int minCodePoint, int maxCodePoint, SpecialTestCases special = SpecialTestCases.None)
        {
            string inputString = GenerateStringData(length, minCodePoint, maxCodePoint, special);
            ReadOnlySpan<byte> utf32 = Text.Encoding.UTF32.GetBytes(inputString);

            var status = Encoders.Utf16.ComputeEncodedBytesFromUtf32(utf32, out int needed);
            Assert.Equal(TransformationStatus.Done, status);

            Span<byte> utf16 = new byte[needed];

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        status = Encoders.Utf16.ConvertFromUtf32(utf32, utf16, out int consumed, out int written);
                        if (status != TransformationStatus.Done)
                            throw new Exception();
                    }
                }
            }
        }

        private static string GenerateStringData(int length, int minCodePoint, int maxCodePoint, SpecialTestCases special = SpecialTestCases.None)
        {
            if (special != SpecialTestCases.None)
            {
                if (special == SpecialTestCases.AlternatingASCIIAndNonASCII) return TextEncoderTestHelper.GenerateStringAlternatingASCIIAndNonASCII(length);
                if (special == SpecialTestCases.MostlyASCIIAndSomeNonASCII) return TextEncoderTestHelper.GenerateStringWithMostlyASCIIAndSomeNonASCII(length);
                return "";
            }
            else
            {
                return TextEncoderTestHelper.GenerateValidString(length, minCodePoint, maxCodePoint);
            }
        }

        public enum SpecialTestCases
        {
            None, AlternatingASCIIAndNonASCII, MostlyASCIIAndSomeNonASCII
        }
    }
}

