// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: stage.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Rv.Data {

  /// <summary>Holder for reflection information generated from stage.proto</summary>
  public static partial class StageReflection {

    #region Descriptor
    /// <summary>File descriptor for stage.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static StageReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "CgtzdGFnZS5wcm90bxIHcnYuZGF0YRoQYmFzaWNUeXBlcy5wcm90bxoLc2xp",
            "ZGUucHJvdG8itwIKBVN0YWdlGlIKBkxheW91dBIbCgR1dWlkGAEgASgLMg0u",
            "cnYuZGF0YS5VVUlEEgwKBG5hbWUYAiABKAkSHQoFc2xpZGUYAyABKAsyDi5y",
            "di5kYXRhLlNsaWRlGmYKCERvY3VtZW50EjIKEGFwcGxpY2F0aW9uX2luZm8Y",
            "ASABKAsyGC5ydi5kYXRhLkFwcGxpY2F0aW9uSW5mbxImCgdsYXlvdXRzGAIg",
            "AygLMhUucnYuZGF0YS5TdGFnZS5MYXlvdXQacgoQU2NyZWVuQXNzaWdubWVu",
            "dBIuCgZzY3JlZW4YASABKAsyHi5ydi5kYXRhLkNvbGxlY3Rpb25FbGVtZW50",
            "VHlwZRIuCgZsYXlvdXQYAiABKAsyHi5ydi5kYXRhLkNvbGxlY3Rpb25FbGVt",
            "ZW50VHlwZWIGcHJvdG8z"));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::Rv.Data.BasicTypesReflection.Descriptor, global::Rv.Data.SlideReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Rv.Data.Stage), global::Rv.Data.Stage.Parser, null, null, null, null, new pbr::GeneratedClrTypeInfo[] { new pbr::GeneratedClrTypeInfo(typeof(global::Rv.Data.Stage.Types.Layout), global::Rv.Data.Stage.Types.Layout.Parser, new[]{ "Uuid", "Name", "Slide" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::Rv.Data.Stage.Types.Document), global::Rv.Data.Stage.Types.Document.Parser, new[]{ "ApplicationInfo", "Layouts" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::Rv.Data.Stage.Types.ScreenAssignment), global::Rv.Data.Stage.Types.ScreenAssignment.Parser, new[]{ "Screen", "Layout" }, null, null, null, null)})
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class Stage : pb::IMessage<Stage>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<Stage> _parser = new pb::MessageParser<Stage>(() => new Stage());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<Stage> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Rv.Data.StageReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public Stage() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public Stage(Stage other) : this() {
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public Stage Clone() {
      return new Stage(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as Stage);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(Stage other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
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
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int CalculateSize() {
      int size = 0;
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(Stage other) {
      if (other == null) {
        return;
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
        }
      }
    }
    #endif

    #region Nested types
    /// <summary>Container for nested types declared in the Stage message type.</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static partial class Types {
      public sealed partial class Layout : pb::IMessage<Layout>
      #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
          , pb::IBufferMessage
      #endif
      {
        private static readonly pb::MessageParser<Layout> _parser = new pb::MessageParser<Layout>(() => new Layout());
        private pb::UnknownFieldSet _unknownFields;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public static pb::MessageParser<Layout> Parser { get { return _parser; } }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public static pbr::MessageDescriptor Descriptor {
          get { return global::Rv.Data.Stage.Descriptor.NestedTypes[0]; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        pbr::MessageDescriptor pb::IMessage.Descriptor {
          get { return Descriptor; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public Layout() {
          OnConstruction();
        }

        partial void OnConstruction();

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public Layout(Layout other) : this() {
          uuid_ = other.uuid_ != null ? other.uuid_.Clone() : null;
          name_ = other.name_;
          slide_ = other.slide_ != null ? other.slide_.Clone() : null;
          _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public Layout Clone() {
          return new Layout(this);
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

        /// <summary>Field number for the "slide" field.</summary>
        public const int SlideFieldNumber = 3;
        private global::Rv.Data.Slide slide_;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public global::Rv.Data.Slide Slide {
          get { return slide_; }
          set {
            slide_ = value;
          }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public override bool Equals(object other) {
          return Equals(other as Layout);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public bool Equals(Layout other) {
          if (ReferenceEquals(other, null)) {
            return false;
          }
          if (ReferenceEquals(other, this)) {
            return true;
          }
          if (!object.Equals(Uuid, other.Uuid)) return false;
          if (Name != other.Name) return false;
          if (!object.Equals(Slide, other.Slide)) return false;
          return Equals(_unknownFields, other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public override int GetHashCode() {
          int hash = 1;
          if (uuid_ != null) hash ^= Uuid.GetHashCode();
          if (Name.Length != 0) hash ^= Name.GetHashCode();
          if (slide_ != null) hash ^= Slide.GetHashCode();
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
          if (slide_ != null) {
            output.WriteRawTag(26);
            output.WriteMessage(Slide);
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
          if (slide_ != null) {
            output.WriteRawTag(26);
            output.WriteMessage(Slide);
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
          if (slide_ != null) {
            size += 1 + pb::CodedOutputStream.ComputeMessageSize(Slide);
          }
          if (_unknownFields != null) {
            size += _unknownFields.CalculateSize();
          }
          return size;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public void MergeFrom(Layout other) {
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
          if (other.slide_ != null) {
            if (slide_ == null) {
              Slide = new global::Rv.Data.Slide();
            }
            Slide.MergeFrom(other.Slide);
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
                if (slide_ == null) {
                  Slide = new global::Rv.Data.Slide();
                }
                input.ReadMessage(Slide);
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
                if (slide_ == null) {
                  Slide = new global::Rv.Data.Slide();
                }
                input.ReadMessage(Slide);
                break;
              }
            }
          }
        }
        #endif

      }

      public sealed partial class Document : pb::IMessage<Document>
      #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
          , pb::IBufferMessage
      #endif
      {
        private static readonly pb::MessageParser<Document> _parser = new pb::MessageParser<Document>(() => new Document());
        private pb::UnknownFieldSet _unknownFields;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public static pb::MessageParser<Document> Parser { get { return _parser; } }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public static pbr::MessageDescriptor Descriptor {
          get { return global::Rv.Data.Stage.Descriptor.NestedTypes[1]; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        pbr::MessageDescriptor pb::IMessage.Descriptor {
          get { return Descriptor; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public Document() {
          OnConstruction();
        }

        partial void OnConstruction();

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public Document(Document other) : this() {
          applicationInfo_ = other.applicationInfo_ != null ? other.applicationInfo_.Clone() : null;
          layouts_ = other.layouts_.Clone();
          _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public Document Clone() {
          return new Document(this);
        }

        /// <summary>Field number for the "application_info" field.</summary>
        public const int ApplicationInfoFieldNumber = 1;
        private global::Rv.Data.ApplicationInfo applicationInfo_;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public global::Rv.Data.ApplicationInfo ApplicationInfo {
          get { return applicationInfo_; }
          set {
            applicationInfo_ = value;
          }
        }

        /// <summary>Field number for the "layouts" field.</summary>
        public const int LayoutsFieldNumber = 2;
        private static readonly pb::FieldCodec<global::Rv.Data.Stage.Types.Layout> _repeated_layouts_codec
            = pb::FieldCodec.ForMessage(18, global::Rv.Data.Stage.Types.Layout.Parser);
        private readonly pbc::RepeatedField<global::Rv.Data.Stage.Types.Layout> layouts_ = new pbc::RepeatedField<global::Rv.Data.Stage.Types.Layout>();
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public pbc::RepeatedField<global::Rv.Data.Stage.Types.Layout> Layouts {
          get { return layouts_; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public override bool Equals(object other) {
          return Equals(other as Document);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public bool Equals(Document other) {
          if (ReferenceEquals(other, null)) {
            return false;
          }
          if (ReferenceEquals(other, this)) {
            return true;
          }
          if (!object.Equals(ApplicationInfo, other.ApplicationInfo)) return false;
          if(!layouts_.Equals(other.layouts_)) return false;
          return Equals(_unknownFields, other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public override int GetHashCode() {
          int hash = 1;
          if (applicationInfo_ != null) hash ^= ApplicationInfo.GetHashCode();
          hash ^= layouts_.GetHashCode();
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
          if (applicationInfo_ != null) {
            output.WriteRawTag(10);
            output.WriteMessage(ApplicationInfo);
          }
          layouts_.WriteTo(output, _repeated_layouts_codec);
          if (_unknownFields != null) {
            _unknownFields.WriteTo(output);
          }
        #endif
        }

        #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
          if (applicationInfo_ != null) {
            output.WriteRawTag(10);
            output.WriteMessage(ApplicationInfo);
          }
          layouts_.WriteTo(ref output, _repeated_layouts_codec);
          if (_unknownFields != null) {
            _unknownFields.WriteTo(ref output);
          }
        }
        #endif

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public int CalculateSize() {
          int size = 0;
          if (applicationInfo_ != null) {
            size += 1 + pb::CodedOutputStream.ComputeMessageSize(ApplicationInfo);
          }
          size += layouts_.CalculateSize(_repeated_layouts_codec);
          if (_unknownFields != null) {
            size += _unknownFields.CalculateSize();
          }
          return size;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public void MergeFrom(Document other) {
          if (other == null) {
            return;
          }
          if (other.applicationInfo_ != null) {
            if (applicationInfo_ == null) {
              ApplicationInfo = new global::Rv.Data.ApplicationInfo();
            }
            ApplicationInfo.MergeFrom(other.ApplicationInfo);
          }
          layouts_.Add(other.layouts_);
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
                if (applicationInfo_ == null) {
                  ApplicationInfo = new global::Rv.Data.ApplicationInfo();
                }
                input.ReadMessage(ApplicationInfo);
                break;
              }
              case 18: {
                layouts_.AddEntriesFrom(input, _repeated_layouts_codec);
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
                if (applicationInfo_ == null) {
                  ApplicationInfo = new global::Rv.Data.ApplicationInfo();
                }
                input.ReadMessage(ApplicationInfo);
                break;
              }
              case 18: {
                layouts_.AddEntriesFrom(ref input, _repeated_layouts_codec);
                break;
              }
            }
          }
        }
        #endif

      }

      public sealed partial class ScreenAssignment : pb::IMessage<ScreenAssignment>
      #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
          , pb::IBufferMessage
      #endif
      {
        private static readonly pb::MessageParser<ScreenAssignment> _parser = new pb::MessageParser<ScreenAssignment>(() => new ScreenAssignment());
        private pb::UnknownFieldSet _unknownFields;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public static pb::MessageParser<ScreenAssignment> Parser { get { return _parser; } }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public static pbr::MessageDescriptor Descriptor {
          get { return global::Rv.Data.Stage.Descriptor.NestedTypes[2]; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        pbr::MessageDescriptor pb::IMessage.Descriptor {
          get { return Descriptor; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public ScreenAssignment() {
          OnConstruction();
        }

        partial void OnConstruction();

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public ScreenAssignment(ScreenAssignment other) : this() {
          screen_ = other.screen_ != null ? other.screen_.Clone() : null;
          layout_ = other.layout_ != null ? other.layout_.Clone() : null;
          _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public ScreenAssignment Clone() {
          return new ScreenAssignment(this);
        }

        /// <summary>Field number for the "screen" field.</summary>
        public const int ScreenFieldNumber = 1;
        private global::Rv.Data.CollectionElementType screen_;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public global::Rv.Data.CollectionElementType Screen {
          get { return screen_; }
          set {
            screen_ = value;
          }
        }

        /// <summary>Field number for the "layout" field.</summary>
        public const int LayoutFieldNumber = 2;
        private global::Rv.Data.CollectionElementType layout_;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public global::Rv.Data.CollectionElementType Layout {
          get { return layout_; }
          set {
            layout_ = value;
          }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public override bool Equals(object other) {
          return Equals(other as ScreenAssignment);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public bool Equals(ScreenAssignment other) {
          if (ReferenceEquals(other, null)) {
            return false;
          }
          if (ReferenceEquals(other, this)) {
            return true;
          }
          if (!object.Equals(Screen, other.Screen)) return false;
          if (!object.Equals(Layout, other.Layout)) return false;
          return Equals(_unknownFields, other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public override int GetHashCode() {
          int hash = 1;
          if (screen_ != null) hash ^= Screen.GetHashCode();
          if (layout_ != null) hash ^= Layout.GetHashCode();
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
          if (screen_ != null) {
            output.WriteRawTag(10);
            output.WriteMessage(Screen);
          }
          if (layout_ != null) {
            output.WriteRawTag(18);
            output.WriteMessage(Layout);
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
          if (screen_ != null) {
            output.WriteRawTag(10);
            output.WriteMessage(Screen);
          }
          if (layout_ != null) {
            output.WriteRawTag(18);
            output.WriteMessage(Layout);
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
          if (screen_ != null) {
            size += 1 + pb::CodedOutputStream.ComputeMessageSize(Screen);
          }
          if (layout_ != null) {
            size += 1 + pb::CodedOutputStream.ComputeMessageSize(Layout);
          }
          if (_unknownFields != null) {
            size += _unknownFields.CalculateSize();
          }
          return size;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
        public void MergeFrom(ScreenAssignment other) {
          if (other == null) {
            return;
          }
          if (other.screen_ != null) {
            if (screen_ == null) {
              Screen = new global::Rv.Data.CollectionElementType();
            }
            Screen.MergeFrom(other.Screen);
          }
          if (other.layout_ != null) {
            if (layout_ == null) {
              Layout = new global::Rv.Data.CollectionElementType();
            }
            Layout.MergeFrom(other.Layout);
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
                if (screen_ == null) {
                  Screen = new global::Rv.Data.CollectionElementType();
                }
                input.ReadMessage(Screen);
                break;
              }
              case 18: {
                if (layout_ == null) {
                  Layout = new global::Rv.Data.CollectionElementType();
                }
                input.ReadMessage(Layout);
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
                if (screen_ == null) {
                  Screen = new global::Rv.Data.CollectionElementType();
                }
                input.ReadMessage(Screen);
                break;
              }
              case 18: {
                if (layout_ == null) {
                  Layout = new global::Rv.Data.CollectionElementType();
                }
                input.ReadMessage(Layout);
                break;
              }
            }
          }
        }
        #endif

      }

    }
    #endregion

  }

  #endregion

}

#endregion Designer generated code