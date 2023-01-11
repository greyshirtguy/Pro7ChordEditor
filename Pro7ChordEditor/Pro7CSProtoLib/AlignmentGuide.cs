// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: alignmentGuide.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Rv.Data {

  /// <summary>Holder for reflection information generated from alignmentGuide.proto</summary>
  public static partial class AlignmentGuideReflection {

    #region Descriptor
    /// <summary>File descriptor for alignmentGuide.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static AlignmentGuideReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "ChRhbGlnbm1lbnRHdWlkZS5wcm90bxIHcnYuZGF0YRoQYmFzaWNUeXBlcy5w",
            "cm90byLkAQoOQWxpZ25tZW50R3VpZGUSGwoEdXVpZBgBIAEoCzINLnJ2LmRh",
            "dGEuVVVJRBJBCgtvcmllbnRhdGlvbhgCIAEoDjIsLnJ2LmRhdGEuQWxpZ25t",
            "ZW50R3VpZGUuR3VpZGVsaW5lT3JpZW50YXRpb24SEAoIbG9jYXRpb24YAyAB",
            "KAEiYAoUR3VpZGVsaW5lT3JpZW50YXRpb24SJAogR1VJREVMSU5FX09SSUVO",
            "VEFUSU9OX0hPUklaT05UQUwQABIiCh5HVUlERUxJTkVfT1JJRU5UQVRJT05f",
            "VkVSVElDQUwQAWIGcHJvdG8z"));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::Rv.Data.BasicTypesReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Rv.Data.AlignmentGuide), global::Rv.Data.AlignmentGuide.Parser, new[]{ "Uuid", "Orientation", "Location" }, null, new[]{ typeof(global::Rv.Data.AlignmentGuide.Types.GuidelineOrientation) }, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class AlignmentGuide : pb::IMessage<AlignmentGuide>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<AlignmentGuide> _parser = new pb::MessageParser<AlignmentGuide>(() => new AlignmentGuide());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<AlignmentGuide> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Rv.Data.AlignmentGuideReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public AlignmentGuide() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public AlignmentGuide(AlignmentGuide other) : this() {
      uuid_ = other.uuid_ != null ? other.uuid_.Clone() : null;
      orientation_ = other.orientation_;
      location_ = other.location_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public AlignmentGuide Clone() {
      return new AlignmentGuide(this);
    }

    /// <summary>Field number for the "uuid" field.</summary>
    public const int UuidFieldNumber = 1;
    private global::Rv.Data.UUID uuid_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::Rv.Data.UUID Uuid {
      get { return uuid_; }
      set {
        uuid_ = value;
      }
    }

    /// <summary>Field number for the "orientation" field.</summary>
    public const int OrientationFieldNumber = 2;
    private global::Rv.Data.AlignmentGuide.Types.GuidelineOrientation orientation_ = global::Rv.Data.AlignmentGuide.Types.GuidelineOrientation.Horizontal;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::Rv.Data.AlignmentGuide.Types.GuidelineOrientation Orientation {
      get { return orientation_; }
      set {
        orientation_ = value;
      }
    }

    /// <summary>Field number for the "location" field.</summary>
    public const int LocationFieldNumber = 3;
    private double location_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public double Location {
      get { return location_; }
      set {
        location_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as AlignmentGuide);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(AlignmentGuide other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!object.Equals(Uuid, other.Uuid)) return false;
      if (Orientation != other.Orientation) return false;
      if (!pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.Equals(Location, other.Location)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (uuid_ != null) hash ^= Uuid.GetHashCode();
      if (Orientation != global::Rv.Data.AlignmentGuide.Types.GuidelineOrientation.Horizontal) hash ^= Orientation.GetHashCode();
      if (Location != 0D) hash ^= pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.GetHashCode(Location);
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void WriteTo(pb::CodedOutputStream output) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
    #else
      if (uuid_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(Uuid);
      }
      if (Orientation != global::Rv.Data.AlignmentGuide.Types.GuidelineOrientation.Horizontal) {
        output.WriteRawTag(16);
        output.WriteEnum((int) Orientation);
      }
      if (Location != 0D) {
        output.WriteRawTag(25);
        output.WriteDouble(Location);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (uuid_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(Uuid);
      }
      if (Orientation != global::Rv.Data.AlignmentGuide.Types.GuidelineOrientation.Horizontal) {
        output.WriteRawTag(16);
        output.WriteEnum((int) Orientation);
      }
      if (Location != 0D) {
        output.WriteRawTag(25);
        output.WriteDouble(Location);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int CalculateSize() {
      int size = 0;
      if (uuid_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Uuid);
      }
      if (Orientation != global::Rv.Data.AlignmentGuide.Types.GuidelineOrientation.Horizontal) {
        size += 1 + pb::CodedOutputStream.ComputeEnumSize((int) Orientation);
      }
      if (Location != 0D) {
        size += 1 + 8;
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(AlignmentGuide other) {
      if (other == null) {
        return;
      }
      if (other.uuid_ != null) {
        if (uuid_ == null) {
          Uuid = new global::Rv.Data.UUID();
        }
        Uuid.MergeFrom(other.Uuid);
      }
      if (other.Orientation != global::Rv.Data.AlignmentGuide.Types.GuidelineOrientation.Horizontal) {
        Orientation = other.Orientation;
      }
      if (other.Location != 0D) {
        Location = other.Location;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(pb::CodedInputStream input) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
    #else
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            if (uuid_ == null) {
              Uuid = new global::Rv.Data.UUID();
            }
            input.ReadMessage(Uuid);
            break;
          }
          case 16: {
            Orientation = (global::Rv.Data.AlignmentGuide.Types.GuidelineOrientation) input.ReadEnum();
            break;
          }
          case 25: {
            Location = input.ReadDouble();
            break;
          }
        }
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 10: {
            if (uuid_ == null) {
              Uuid = new global::Rv.Data.UUID();
            }
            input.ReadMessage(Uuid);
            break;
          }
          case 16: {
            Orientation = (global::Rv.Data.AlignmentGuide.Types.GuidelineOrientation) input.ReadEnum();
            break;
          }
          case 25: {
            Location = input.ReadDouble();
            break;
          }
        }
      }
    }
    #endif

    #region Nested types
    /// <summary>Container for nested types declared in the AlignmentGuide message type.</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static partial class Types {
      public enum GuidelineOrientation {
        [pbr::OriginalName("GUIDELINE_ORIENTATION_HORIZONTAL")] Horizontal = 0,
        [pbr::OriginalName("GUIDELINE_ORIENTATION_VERTICAL")] Vertical = 1,
      }

    }
    #endregion

  }

  #endregion

}

#endregion Designer generated code