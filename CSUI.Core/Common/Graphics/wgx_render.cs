// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Runtime.InteropServices;
using SR = MS.Internal.PresentationCore.SR;

namespace MS.Internal
{
    internal enum NtStatusErrors
    {
        NT_STATUS_NO_MEMORY = unchecked((int)0xC0000017)
    }

    internal enum LockFlags
    {
        MIL_LOCK_READ  = 0x00000001,
        MIL_LOCK_WRITE = 0x00000002,
    }

    internal enum MILRTInitializationFlags
    {
        MIL_RT_INITIALIZE_DEFAULT = 0x00000000,
    }

    internal static class HRESULT
    {
        internal const int S_OK = 0;
        internal const int E_FAIL = unchecked((int)0x80004005);
        internal const int E_OUTOFMEMORY = unchecked((int)0x8007000E);
        internal const int E_INVALIDARG = unchecked((int)0x80070057);
        internal const int D3DERR_OUTOFVIDEOMEMORY = unchecked((int)0x8876017C);

        internal const int FACILITY_NT_BIT = 0x10000000;
        internal const int FACILITY_MASK = 0x7FFF0000;
        internal const int FACILITY_WINCODEC_ERROR = 0x08980000;
        internal const int COMPONENT_MASK = 0x0000E000;
        internal const int COMPONENT_WINCODEC_ERROR = 0x00002000;

        internal static bool IsWindowsCodecError(int hr)
        {
            return (hr & (FACILITY_MASK | COMPONENT_MASK)) == (FACILITY_WINCODEC_ERROR | COMPONENT_WINCODEC_ERROR);
        }

        internal static Exception ConvertHRToException(int hr)
        {
            Exception exceptionForHR = Marshal.GetExceptionForHR(hr, (IntPtr)(-1));

            if ((hr & FACILITY_NT_BIT) == FACILITY_NT_BIT)
            {
                return (hr & ~FACILITY_NT_BIT) switch
                {
                    (int)NtStatusErrors.NT_STATUS_NO_MEMORY => new OutOfMemoryException(),
                    _ => exceptionForHR,
                };
            }

            switch (hr)
            {
                case (int)NtStatusErrors.NT_STATUS_NO_MEMORY:
                    return new OutOfMemoryException();

                case (int)WinCodecErrors.WINCODEC_ERR_WRONGSTATE:
                    return new InvalidOperationException(SR.Image_WrongState, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_VALUEOUTOFRANGE:
                case (int)WinCodecErrors.WINCODEC_ERR_VALUEOVERFLOW:
                    return new OverflowException(SR.Image_Overflow, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_UNKNOWNIMAGEFORMAT:
                    return new System.IO.FileFormatException(null, SR.Image_UnknownFormat, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDVERSION:
                    return new System.IO.FileLoadException(SR.MilErr_UnsupportedVersion, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_NOTINITIALIZED:
                    return new InvalidOperationException(SR.WIC_NotInitialized, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_PROPERTYNOTFOUND:
                    return new ArgumentException(SR.Image_PropertyNotFound, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_PROPERTYNOTSUPPORTED:
                    return new NotSupportedException(SR.Image_PropertyNotSupported, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_PROPERTYSIZE:
                    return new ArgumentException(SR.Image_PropertySize, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_CODECPRESENT:
                    return new InvalidOperationException(SR.Image_CodecPresent, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_CODECNOTHUMBNAIL:
                    return new NotSupportedException(SR.Image_NoThumbnail, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_PALETTEUNAVAILABLE:
                    return new InvalidOperationException(SR.Image_NoPalette, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_CODECTOOMANYSCANLINES:
                    return new ArgumentException(SR.Image_TooManyScanlines, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_INTERNALERROR:
                    return new InvalidOperationException(SR.Image_InternalError, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_SOURCERECTDOESNOTMATCHDIMENSIONS:
                    return new ArgumentException(SR.Image_BadDimensions, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_COMPONENTINITIALIZEFAILURE:
                case (int)WinCodecErrors.WINCODEC_ERR_COMPONENTNOTFOUND:
                    return new NotSupportedException(SR.Image_ComponentNotFound, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_UNEXPECTEDSIZE:
                case (int)WinCodecErrors.WINCODEC_ERR_BADIMAGE:
                    return new System.IO.FileFormatException(null, SR.Image_DecoderError, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_BADHEADER:
                    return new System.IO.FileFormatException(null, SR.Image_HeaderError, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_FRAMEMISSING:
                    return new ArgumentException(SR.Image_FrameMissing, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_BADMETADATAHEADER:
                    return new ArgumentException(SR.Image_BadMetadataHeader, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_BADSTREAMDATA:
                    return new ArgumentException(SR.Image_BadStreamData, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_STREAMWRITE:
                    return new InvalidOperationException(SR.Image_StreamWrite, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDPIXELFORMAT:
                    return new NotSupportedException(SR.Image_UnsupportedPixelFormat, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDOPERATION:
                    return new NotSupportedException(SR.Image_UnsupportedOperation, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_IMAGESIZEOUTOFRANGE:
                    return new ArgumentException(SR.Image_SizeOutOfRange, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_STREAMREAD:
                    return new System.IO.IOException(SR.Image_StreamRead, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_INVALIDQUERYREQUEST:
                    return new System.IO.IOException(SR.Image_InvalidQueryRequest, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_UNEXPECTEDMETADATATYPE:
                    return new System.IO.FileFormatException(null, SR.Image_UnexpectedMetadataType, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_REQUESTONLYVALIDATMETADATAROOT:
                    return new System.IO.FileFormatException(null, SR.Image_RequestOnlyValidAtMetadataRoot, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_INVALIDQUERYCHARACTER:
                    return new System.IO.IOException(SR.Image_InvalidQueryCharacter, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_DUPLICATEMETADATAPRESENT:
                    return new System.IO.FileFormatException(null, SR.Image_DuplicateMetadataPresent, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_PROPERTYUNEXPECTEDTYPE:
                    return new System.IO.FileFormatException(null, SR.Image_PropertyUnexpectedType, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_TOOMUCHMETADATA:
                    return new System.IO.FileFormatException(null, SR.Image_TooMuchMetadata, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_STREAMNOTAVAILABLE:
                    return new NotSupportedException(SR.Image_StreamNotAvailable, exceptionForHR);

                case (int)WinCodecErrors.WINCODEC_ERR_INSUFFICIENTBUFFER:
                    return new ArgumentException(SR.Image_InsufficientBuffer, exceptionForHR);

                case E_INVALIDARG:
                    return new ArgumentException(SR.Format(SR.Media_InvalidArgument, null), exceptionForHR);

                case unchecked((int)0x800707db):
                    return new System.IO.FileFormatException(null, SR.Image_InvalidColorContext, exceptionForHR);

                case (int)MILErrors.WGXERR_DISPLAYSTATEINVALID:
                    return new InvalidOperationException(SR.Image_DisplayStateInvalid, exceptionForHR);

                case (int)MILErrors.WGXERR_NONINVERTIBLEMATRIX:
                    return new ArithmeticException(SR.Image_SingularMatrix, exceptionForHR);

                case (int)MILErrors.WGXERR_BADNUMBER:
                    return new ArithmeticException(SR.Geometry_BadNumber, exceptionForHR);

                case (int)MILErrors.WGXERR_D3DI_INVALIDSURFACEUSAGE:
                    return new ArgumentException(SR.D3DImage_InvalidUsage, exceptionForHR);

                case (int)MILErrors.WGXERR_D3DI_INVALIDSURFACESIZE:
                    return new ArgumentException(SR.D3DImage_SurfaceTooBig, exceptionForHR);

                case (int)MILErrors.WGXERR_D3DI_INVALIDSURFACEPOOL:
                    return new ArgumentException(SR.D3DImage_InvalidPool, exceptionForHR);

                case (int)MILErrors.WGXERR_D3DI_INVALIDSURFACEDEVICE:
                    return new ArgumentException(SR.D3DImage_InvalidDevice, exceptionForHR);

                case (int)MILErrors.WGXERR_D3DI_INVALIDANTIALIASINGSETTINGS:
                    return new ArgumentException(SR.D3DImage_AARequires9Ex, exceptionForHR);

                default:
                    return exceptionForHR;
            }
        }

        public static void Check(int hr)
        {
            if (hr < 0)
            {
                throw ConvertHRToException(hr);
            }
        }

        public static bool Succeeded(int hr) => hr >= 0;

        public static bool Failed(int hr) => hr < 0;
    }
}
