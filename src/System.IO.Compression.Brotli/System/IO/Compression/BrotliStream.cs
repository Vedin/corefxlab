﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression.Resources;
using System.Runtime.InteropServices;

#if BIT64
    using nuint = System.UInt64;
#else
    using nuint = System.UInt32;
#endif 

namespace System.IO.Compression
{
    public partial class BrotliStream : Stream
    {
        private const int DefaultBufferSize = (1 << 16) - 1;
        private int _bufferSize;
        private Stream _stream;
        private CompressionMode _mode;
        private int _availableOutput;
        private int _availableInput;
        private byte[] _nextInput;
        private byte[] _bufferOutput;
        private bool _leaveOpen;
        private int totalWrote;
        private Brotli.State _state;
        private TransformationStatus transformationResult;

        internal Stream BufferStream => _bufferStream;
        private MemoryStream _bufferStream;

        public override bool CanTimeout => true;

        public override int ReadTimeout { get; set; }

        public override int WriteTimeout { get; set; }

        public BrotliStream(Stream baseStream, CompressionMode mode, bool leaveOpen, int bufferSize, CompressionLevel quality) : this(baseStream, mode, leaveOpen, bufferSize)
        {
            if (_mode == CompressionMode.Decompress)
            {
                throw new System.IO.IOException(BrotliEx.QualityAndWinSize);
            }
            else
            {
                _state.SetQuality((uint)Brotli.GetQualityFromCompressionLevel(quality));
            }
        }

        public BrotliStream(Stream baseStream, CompressionMode mode, bool leaveOpen, int bufferSize, CompressionLevel quality, uint windowSize) : this(baseStream, mode, leaveOpen, bufferSize)
        {
            if (_mode == CompressionMode.Decompress)
            {
                throw new System.IO.IOException(BrotliEx.QualityAndWinSize);
            }
            else
            {
                _state.SetQuality((uint)Brotli.GetQualityFromCompressionLevel(quality));
                _state.SetWindow(windowSize);
            }
        }

        public BrotliStream(Stream baseStream, CompressionMode mode, bool leaveOpen = false, int bufferSize = DefaultBufferSize)
        {
            if (baseStream == null)
            {
                throw new ArgumentNullException("baseStream");
            }
            _mode = mode;
            _stream = baseStream;
            _leaveOpen = leaveOpen;
            _state = new Brotli.State();
            if (_mode == CompressionMode.Compress)
            {
                try
                {
                    _state.InitializeEncoder();
                }
                catch (IO.IOException exception)
                {
                    throw exception;
                }
                _state.SetQuality();
                _state.SetWindow();
                WriteTimeout = 0;
            }
            else
            {
                try
                {
                    _state.InitializeDecoder();
                }
                catch (IO.IOException exception)
                {
                    throw exception;
                }
                _bufferStream = new MemoryStream();
                ReadTimeout = 0;
            }
            transformationResult = TransformationStatus.NeedMoreSourceData;
            _bufferSize = bufferSize;
            _bufferOutput = new byte[_bufferSize];
            _nextInput = new byte[_bufferSize];
            _availableOutput = _bufferSize;
        }

        public override bool CanRead
        {
            get
            {
                if (_stream == null)
                {
                    return false;
                }
                return (_mode == CompressionMode.Decompress && _stream.CanRead);
            }
        }

        public override bool CanWrite
        {
            get
            {
                if (_stream == null)
                {
                    return false;
                }
                return (_mode == CompressionMode.Compress && _stream.CanWrite);
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override bool CanSeek => false;

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        protected virtual void FlushEncoder(bool finished)
        {
            if (_state.BrotliNativeState == IntPtr.Zero) return;
            if (BrotliNative.BrotliEncoderIsFinished(_state.BrotliNativeState)) return;
            _nextInput = new byte[0];
            _availableInput = 0;
            TransformationStatus flushStatus = TransformationStatus.DestinationTooSmall;
            while (flushStatus == TransformationStatus.DestinationTooSmall)
            {
                flushStatus = Brotli.FlushEncoder(_nextInput, _bufferOutput, out _availableInput, out _availableOutput, ref _state, finished);
                _stream.Write(_bufferOutput, 0, _availableOutput);
                _availableOutput = _bufferSize;

                if (BrotliNative.BrotliEncoderIsFinished(_state.BrotliNativeState))
                {
                    break;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _stream != null && _mode == CompressionMode.Compress)
            {
                FlushEncoder(true);
            }
            try
            {
                if (disposing && !_leaveOpen) _stream?.Dispose();
            }
            finally
            {
                _stream = null;
                _state.Dispose();
                base.Dispose(disposing);
            }
        }

        public override void Flush()
        {
            EnsureNotDisposed();
            if (_mode == CompressionMode.Compress)
            {
                FlushEncoder(false);
            }
        }

        private void ValidateParameters(byte[] array, int offset, int count)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (array.Length - offset < count)
                throw new ArgumentOutOfRangeException("Offset and Count aren't consistent", BrotliEx.InvalidArgument);
        }

        private void EnsureDecompressionMode()
        {
            if (_mode != CompressionMode.Decompress)
                throw new System.InvalidOperationException(BrotliEx.WrongModeDecompress);
        }

        private void EnsureNotDisposed()
        {
            if (_stream == null)
                throw new ObjectDisposedException(BrotliEx.StreamDisposed);
        }

        internal void RemoveBytes(int numberOfBytes)
        {
            ArraySegment<byte> buf;
            if (_bufferStream.TryGetBuffer(out buf))
            {
                Buffer.BlockCopy(buf.Array, numberOfBytes, buf.Array, 0, (int)_bufferStream.Length - numberOfBytes);
                _bufferStream.SetLength(BufferStream.Length - numberOfBytes);
            }
            else
            {
                throw new UnauthorizedAccessException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            EnsureDecompressionMode();
            ValidateParameters(buffer, offset, count);
            EnsureNotDisposed();
            DateTime begin = DateTime.Now;
            _availableOutput = 0;
            Byte[] buf = new Byte[_bufferSize];
            TimeSpan ExecutionTime = DateTime.Now - begin;
            if (ReadTimeout > 0 && ExecutionTime.TotalMilliseconds >= ReadTimeout)
            {
                throw new TimeoutException(BrotliEx.TimeoutRead);
            }
            while (true)
            {
                if (transformationResult == TransformationStatus.NeedMoreSourceData)
                {
                    _availableInput = _stream.Read(_nextInput, 0, _bufferSize);
                    if ((int)_availableInput <= 0)
                    {
                        break;
                    }
                }
                else if (transformationResult != TransformationStatus.DestinationTooSmall)
                {
                    break;
                }
                transformationResult = Brotli.Decompress(_nextInput, buffer, out _availableInput, out _availableOutput, ref _state);
                if (_availableOutput != 0)
                {
                    return _availableOutput;
                }
            }
            return 0;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        private void EnsureCompressionMode()
        {
            if (_mode != CompressionMode.Compress)
                throw new System.InvalidOperationException(BrotliEx.WrongModeCompress);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            EnsureCompressionMode();
            ValidateParameters(buffer, offset, count);
            EnsureNotDisposed();
            if (_mode != CompressionMode.Compress)
                totalWrote += count;
            DateTime begin = DateTime.Now;
            int bytesRemain = count;
            int currentOffset = offset;
            int copyLen;
            while (bytesRemain > 0)
            {
                TimeSpan ExecutionTime = DateTime.Now - begin;
                if (WriteTimeout > 0 && ExecutionTime.TotalMilliseconds >= WriteTimeout)
                {
                    throw new TimeoutException(BrotliEx.TimeoutWrite);
                }
                copyLen = bytesRemain > _bufferSize ? _bufferSize : bytesRemain;
                byte[] bufferInput = new byte[copyLen];
                Array.Copy(buffer, currentOffset, bufferInput, 0, copyLen);
                TransformationStatus transformationResult = TransformationStatus.DestinationTooSmall;
                /*Span<byte> bufferInput = new Span<byte>(buffer);
                bufferInput.Slice(0, copyLen);*/
                transformationResult = Brotli.Compress(bufferInput, _bufferOutput, out _availableInput, out _availableOutput, ref _state);
                if (transformationResult == TransformationStatus.InvalidData) throw new System.IO.IOException(BrotliEx.unableEncode);
                if (transformationResult == TransformationStatus.DestinationTooSmall)
                {
                    _stream.Write(_bufferOutput, 0, _availableOutput);
                    copyLen = _availableInput;
                }
                bytesRemain -= copyLen;
                currentOffset += copyLen;
            }
        }
    }

}
