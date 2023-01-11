// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: groups.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Rv.Data {

  /// <summary>Holder for reflection information generated from groups.proto</summary>
  public static partial class GroupsReflection {

    #region Descriptor
    /// <summary>File descriptor for groups.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static GroupsReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "Cgxncm91cHMucHJvdG8SB3J2LmRhdGEaEGJhc2ljVHlwZXMucHJvdG8aDGhv",
            "dEtleS5wcm90byLHAQoFR3JvdXASGwoEdXVpZBgBIAEoCzINLnJ2LmRhdGEu",
            "VVVJRBIMCgRuYW1lGAIgASgJEh0KBWNvbG9yGAMgASgLMg4ucnYuZGF0YS5D",
            "b2xvchIfCgZob3RLZXkYBCABKAsyDy5ydi5kYXRhLkhvdEtleRIzChxhcHBs",
            "aWNhdGlvbl9ncm91cF9pZGVudGlmaWVyGAUgASgLMg0ucnYuZGF0YS5VVUlE",
            "Eh4KFmFwcGxpY2F0aW9uX2dyb3VwX25hbWUYBiABKAkiMwoRUHJvR3JvdXBz",
            "RG9jdW1lbnQSHgoGZ3JvdXBzGAEgAygLMg4ucnYuZGF0YS5Hcm91cGIGcHJv",
            "dG8z"));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::Rv.Data.BasicTypesReflection.Descriptor, global::Rv.Data.HotKeyReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Rv.Data.Group), global::Rv.Data.Group.Parser, new[]{ "Uuid", "Name", "Color", "HotKey", "ApplicationGroupIdentifier", "ApplicationGroupName" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::Rv.Data.ProGroupsDocument), global::Rv.Data.ProGroupsDocument.Parser, new[]{ "Groups" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class Group : pb::IMessage<Group>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<Group> _parser = new pb::MessageParser<Group>(() => new Group());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<Group> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Rv.Data.GroupsReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public Group() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public Group(Group other) : this() {
      uuid_ = other.uuid_ != null ? other.uuid_.Clone() : null;
      name_ = other.name_;
      color_ = other.color_ != null ? other.color_.Clone() : null;
      hotKey_ = other.hotKey_ != null ? other.hotKey_.Clone() : null;
      applicationGroupIdentifier_ = other.applicationGroupIdentifier_ != null ? other.applicationGroupIdentifier_.Clone() : null;
      applicationGroupName_ = other.applicationGroupName_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public Group Clone() {
      return new Group(this);
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

    /// <summary>Field number for the "name" field.</summary>
    public const int NameFieldNumber = 2;
    private string name_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public string Name {
      get { return name_; }
      set {
        name_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "color" field.</summary>
    public const int ColorFieldNumber = 3;
    private global::Rv.Data.Color color_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::Rv.Data.Color Color {
      get { return color_; }
      set {
        color_ = value;
      }
    }

    /// <summary>Field number for the "hotKey" field.</summary>
    public const int HotKeyFieldNumber = 4;
    private global::Rv.Data.HotKey hotKey_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::Rv.Data.HotKey HotKey {
      get { return hotKey_; }
      set {
        hotKey_ = value;
      }
    }

    /// <summary>Field number for the "application_group_identifier" field.</summary>
    public const int ApplicationGroupIdentifierFieldNumber = 5;
    private global::Rv.Data.UUID applicationGroupIdentifier_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::Rv.Data.UUID ApplicationGroupIdentifier {
      get { return applicationGroupIdentifier_; }
      set {
        applicationGroupIdentifier_ = value;
      }
    }

    /// <summary>Field number for the "application_group_name" field.</summary>
    public const int ApplicationGroupNameFieldNumber = 6;
    private string applicationGroupName_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public string ApplicationGroupName {
      get { return applicationGroupName_; }
      set {
        applicationGroupName_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as Group);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(Group other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!object.Equals(Uuid, other.Uuid)) return false;
      if (Name != other.Name) return false;
      if (!object.Equals(Color, other.Color)) return false;
      if (!object.Equals(HotKey, other.HotKey)) return false;
      if (!object.Equals(ApplicationGroupIdentifier, other.ApplicationGroupIdentifier)) return false;
      if (ApplicationGroupName != other.ApplicationGroupName) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (uuid_ != null) hash ^= Uuid.GetHashCode();
      if (Name.Length != 0) hash ^= Name.GetHashCode();
      if (color_ != null) hash ^= Color.GetHashCode();
      if (hotKey_ != null) hash ^= HotKey.GetHashCode();
      if (applicationGroupIdentifier_ != null) hash ^= ApplicationGroupIdentifier.GetHashCode();
      if (ApplicationGroupName.Length != 0) hash ^= ApplicationGroupName.GetHashCode();
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
      if (Name.Length != 0) {
        output.WriteRawTag(18);
        output.WriteString(Name);
      }
      if (color_ != null) {
        output.WriteRawTag(26);
        output.WriteMessage(Color);
      }
      if (hotKey_ != null) {
        output.WriteRawTag(34);
        output.WriteMessage(HotKey);
      }
      if (applicationGroupIdentifier_ != null) {
        output.WriteRawTag(42);
        output.WriteMessage(ApplicationGroupIdentifier);
      }
      if (ApplicationGroupName.Length != 0) {
        output.WriteRawTag(50);
        output.WriteString(ApplicationGroupName);
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
      if (Name.Length != 0) {
        output.WriteRawTag(18);
        output.WriteString(Name);
      }
      if (color_ != null) {
        output.WriteRawTag(26);
        output.WriteMessage(Color);
      }
      if (hotKey_ != null) {
        output.WriteRawTag(34);
        output.WriteMessage(HotKey);
      }
      if (applicationGroupIdentifier_ != null) {
        output.WriteRawTag(42);
        output.WriteMessage(ApplicationGroupIdentifier);
      }
      if (ApplicationGroupName.Length != 0) {
        output.WriteRawTag(50);
        output.WriteString(ApplicationGroupName);
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
      if (Name.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Name);
      }
      if (color_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Color);
      }
      if (hotKey_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(HotKey);
      }
      if (applicationGroupIdentifier_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(ApplicationGroupIdentifier);
      }
      if (ApplicationGroupName.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(ApplicationGroupName);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(Group other) {
      if (other == null) {
        return;
      }
      if (other.uuid_ != null) {
        if (uuid_ == null) {
          Uuid = new global::Rv.Data.UUID();
        }
        Uuid.MergeFrom(other.Uuid);
      }
      if (other.Name.Length != 0) {
        Name = other.Name;
      }
      if (other.color_ != null) {
        if (color_ == null) {
          Color = new global::Rv.Data.Color();
        }
        Color.MergeFrom(other.Color);
      }
      if (other.hotKey_ != null) {
        if (hotKey_ == null) {
          HotKey = new global::Rv.Data.HotKey();
        }
        HotKey.MergeFrom(other.HotKey);
      }
      if (other.applicationGroupIdentifier_ != null) {
        if (applicationGroupIdentifier_ == null) {
          ApplicationGroupIdentifier = new global::Rv.Data.UUID();
        }
        ApplicationGroupIdentifier.MergeFrom(other.ApplicationGroupIdentifier);
      }
      if (other.ApplicationGroupName.Length != 0) {
        ApplicationGroupName = other.ApplicationGroupName;
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
          case 18: {
            Name = input.ReadString();
            break;
          }
          case 26: {
            if (color_ == null) {
              Color = new global::Rv.Data.Color();
            }
            input.ReadMessage(Color);
            break;
          }
          case 34: {
            if (hotKey_ == null) {
              HotKey = new global::Rv.Data.HotKey();
            }
            input.ReadMessage(HotKey);
            break;
          }
          case 42: {
            if (applicationGroupIdentifier_ == null) {
              ApplicationGroupIdentifier = new global::Rv.Data.UUID();
            }
            input.ReadMessage(ApplicationGroupIdentifier);
            break;
          }
          case 50: {
            ApplicationGroupName = input.ReadString();
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
          case 18: {
            Name = input.ReadString();
            break;
          }
          case 26: {
            if (color_ == null) {
              Color = new global::Rv.Data.Color();
            }
            input.ReadMessage(Color);
            break;
          }
          case 34: {
            if (hotKey_ == null) {
              HotKey = new global::Rv.Data.HotKey();
            }
            input.ReadMessage(HotKey);
            break;
          }
          case 42: {
            if (applicationGroupIdentifier_ == null) {
              ApplicationGroupIdentifier = new global::Rv.Data.UUID();
            }
            input.ReadMessage(ApplicationGroupIdentifier);
            break;
          }
          case 50: {
            ApplicationGroupName = input.ReadString();
            break;
          }
        }
      }
    }
    #endif

  }

  public sealed partial class ProGroupsDocument : pb::IMessage<ProGroupsDocument>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<ProGroupsDocument> _parser = new pb::MessageParser<ProGroupsDocument>(() => new ProGroupsDocument());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<ProGroupsDocument> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Rv.Data.GroupsReflection.Descriptor.MessageTypes[1]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ProGroupsDocument() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ProGroupsDocument(ProGroupsDocument other) : this() {
      groups_ = other.groups_.Clone();
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ProGroupsDocument Clone() {
      return new ProGroupsDocument(this);
    }

    /// <summary>Field number for the "groups" field.</summary>
    public const int GroupsFieldNumber = 1;
    private static readonly pb::FieldCodec<global::Rv.Data.Group> _repeated_groups_codec
        = pb::FieldCodec.ForMessage(10, global::Rv.Data.Group.Parser);
    private readonly pbc::RepeatedField<global::Rv.Data.Group> groups_ = new pbc::RepeatedField<global::Rv.Data.Group>();
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public pbc::RepeatedField<global::Rv.Data.Group> Groups {
      get { return groups_; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as ProGroupsDocument);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(ProGroupsDocument other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if(!groups_.Equals(other.groups_)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      hash ^= groups_.GetHashCode();
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
      groups_.WriteTo(output, _repeated_groups_codec);
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      groups_.WriteTo(ref output, _repeated_groups_codec);
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int CalculateSize() {
      int size = 0;
      size += groups_.CalculateSize(_repeated_groups_codec);
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(ProGroupsDocument other) {
      if (other == null) {
        return;
      }
      groups_.Add(other.groups_);
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
            groups_.AddEntriesFrom(input, _repeated_groups_codec);
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
            groups_.AddEntriesFrom(ref input, _repeated_groups_codec);
            break;
          }
        }
      }
    }
    #endif

  }

  #endregion

}

#endregion Designer generated code