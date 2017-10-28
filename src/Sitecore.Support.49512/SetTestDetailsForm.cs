using Sitecore.Analytics.Data.Items;
using Sitecore.Analytics.Testing.TestingUtils;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Comparers;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Layouts;
using Sitecore.Pipelines;
using Sitecore.Pipelines.GetPlaceholderRenderings;
using Sitecore.Pipelines.GetRenderingDatasource;
using Sitecore.Resources;
using Sitecore.Shell.Applications.Dialogs.Testing;
using Sitecore.Shell.Controls;
using Sitecore.sitecore.shell.Controls.DatasourceExamples;
using Sitecore.StringExtensions;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Web.UI;
using Sitecore.Web.UI;

namespace Sitecore.Support.Shell.Applications.Dialogs.Testing
{
  public class SetTestDetailsForm : DialogForm
  {
    [Serializable]
    private class VariableValueItemStub
    {
      public string Datasource;

      public bool HideComponent;

      public string Name;

      public string ReplacementComponent;

      private readonly string id;

      public ID Id
      {
        get
        {
          return string.IsNullOrEmpty(this.id) ? ID.Null : ShortID.DecodeID(this.id);
        }
      }

      public VariableValueItemStub(ID id, string name)
      {
        Assert.ArgumentNotNull(id, "id");
        Assert.ArgumentNotNull(name, "name");
        this.Datasource = string.Empty;
        this.HideComponent = false;
        this.ReplacementComponent = string.Empty;
        this.Name = name;
        this.id = id.ToShortID().ToString();
      }
    }

    protected Checkbox ComponentReplacing;

    protected Button NewVariation;

    protected Border NoVariations;

    protected Border ResetContainer;

    protected Border Variations;

    private static readonly string NewVariationDefaultName = "Variation Name";

    private DeviceDefinition device;

    private LayoutDefinition layout;

    private RenderingDefinition rendering;

    protected ItemUri ContextItemUri
    {
      get
      {
        return base.ServerProperties["itemUri"] as ItemUri;
      }
      set
      {
        base.ServerProperties["itemUri"] = value;
      }
    }

    protected DeviceDefinition Device
    {
      get
      {
        if (this.device == null)
        {
          LayoutDefinition layoutDefinition = this.Layout;
          if (layoutDefinition != null && !string.IsNullOrEmpty(this.DeviceId))
          {
            this.device = layoutDefinition.GetDevice(this.DeviceId);
          }
        }
        return this.device;
      }
    }

    protected string DeviceId
    {
      get
      {
        return base.ServerProperties["deviceid"] as string;
      }
      set
      {
        Assert.IsNotNullOrEmpty(value, "value");
        base.ServerProperties["deviceid"] = value;
      }
    }

    protected LayoutDefinition Layout
    {
      get
      {
        if (this.layout == null)
        {
          string sessionString = WebUtil.GetSessionString(this.LayoutSessionHandle);
          if (!string.IsNullOrEmpty(sessionString))
          {
            this.layout = LayoutDefinition.Parse(sessionString);
          }
        }
        return this.layout;
      }
    }

    protected string LayoutSessionHandle
    {
      get
      {
        return base.ServerProperties["lsh"] as string;
      }
      set
      {
        Assert.IsNotNullOrEmpty(value, "value");
        base.ServerProperties["lsh"] = value;
      }
    }

    protected RenderingDefinition Rendering
    {
      get
      {
        if (this.rendering == null)
        {
          DeviceDefinition deviceDefinition = this.Device;
          string renderingUniqueId = this.RenderingUniqueId;
          if (deviceDefinition != null && !string.IsNullOrEmpty(renderingUniqueId))
          {
            this.rendering = deviceDefinition.GetRenderingByUniqueId(renderingUniqueId);
          }
        }
        return this.rendering;
      }
    }

    protected string RenderingUniqueId
    {
      get
      {
        return base.ServerProperties["renderingid"] as string;
      }
      set
      {
        Assert.IsNotNullOrEmpty(value, "value");
        base.ServerProperties["renderingid"] = value;
      }
    }

    private List<SetTestDetailsForm.VariableValueItemStub> VariableValues
    {
      get
      {
        List<SetTestDetailsForm.VariableValueItemStub> list = base.ServerProperties["variables"] as List<SetTestDetailsForm.VariableValueItemStub>;
        return list ?? new List<SetTestDetailsForm.VariableValueItemStub>();
      }
      set
      {
        Assert.IsNotNull(value, "value");
        base.ServerProperties["variables"] = value;
      }
    }

    [UsedImplicitly]
    protected void AddVariation()
    {
      ID newID = ID.NewID;
      SetTestDetailsForm.VariableValueItemStub variableValueItemStub = new SetTestDetailsForm.VariableValueItemStub(newID, Translate.Text(SetTestDetailsForm.NewVariationDefaultName));
      List<SetTestDetailsForm.VariableValueItemStub> variableValues = this.VariableValues;
      variableValues.Insert(0, variableValueItemStub);
      this.VariableValues = variableValues;
      string text = this.RenderVariableValue(variableValueItemStub);
      this.SetControlsState();
      SheerResponse.Insert(this.Variations.ClientID, "afterBegin", text);
      SheerResponse.Eval("Sitecore.CollapsiblePanel.newAdded('{0}')".FormatWith(new object[]
      {
                newID.ToShortID()
      }));
    }

    protected void AllowComponentReplace()
    {
      if (!this.ComponentReplacing.Checked)
      {
        if (this.VariableValues.FindIndex((SetTestDetailsForm.VariableValueItemStub v) => !string.IsNullOrEmpty(v.ReplacementComponent)) >= 0)
        {
          NameValueCollection nameValueCollection = new NameValueCollection();
          Context.ClientPage.Start(this, "ShowConfirm", nameValueCollection);
          return;
        }
      }
      SheerResponse.Eval("scToggleTestComponentSection()");
    }

    [UsedImplicitly]
    protected void ChangeDisplayComponent(string variationId)
    {
      Assert.ArgumentNotNull(variationId, "variationId");
      ID id = ShortID.DecodeID(variationId);
      List<SetTestDetailsForm.VariableValueItemStub> variableValues = this.VariableValues;
      SetTestDetailsForm.VariableValueItemStub variableValueItemStub = variableValues.Find((SetTestDetailsForm.VariableValueItemStub v) => v.Id == id);
      if (variableValueItemStub != null)
      {
        variableValueItemStub.HideComponent = !variableValueItemStub.HideComponent;
        using (HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter()))
        {
          this.RenderContentControls(htmlTextWriter, variableValueItemStub);
          SheerResponse.SetOuterHtml(variationId + "_content", htmlTextWriter.InnerWriter.ToString());
        }
        using (HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter()))
        {
          this.RenderComponentControls(htmlTextWriter, variableValueItemStub);
          SheerResponse.SetOuterHtml(variationId + "_component", htmlTextWriter.InnerWriter.ToString());
        }
        this.VariableValues = variableValues;
      }
    }

    protected virtual void InitVariableValues()
    {
      if (this.Rendering != null)
      {
        IEnumerable<MultivariateTestValueItem> variableValues = TestingUtil.MultiVariateTesting.GetVariableValues(this.Rendering, Client.ContentDatabase);
        List<SetTestDetailsForm.VariableValueItemStub> list = new List<SetTestDetailsForm.VariableValueItemStub>();
        foreach (MultivariateTestValueItem current in variableValues)
        {
          SetTestDetailsForm.VariableValueItemStub variableValueItemStub = new SetTestDetailsForm.VariableValueItemStub(current.ID, current.Name)
          {
            Datasource = ((current.Datasource.Uri != null && current.Datasource.Uri.ItemID != ID.Null) ? current.Datasource.Uri.ItemID.ToString() : string.Empty),
            HideComponent = current.HideComponent
          };
          variableValueItemStub.ReplacementComponent = ((current.ReplacementComponent.Uri != null && current.ReplacementComponent.Uri.ItemID != ID.Null) ? current.ReplacementComponent.Uri.ItemID.ToString() : string.Empty);
          list.Add(variableValueItemStub);
        }
        this.VariableValues = list;
      }
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);
      if (!Context.ClientPage.IsEvent)
      {
        SetTestDetailsOptions setTestDetailsOptions = SetTestDetailsOptions.Parse();
        this.DeviceId = setTestDetailsOptions.DeviceId;
        this.ContextItemUri = ItemUri.Parse(setTestDetailsOptions.ItemUri);
        this.RenderingUniqueId = setTestDetailsOptions.RenderingUniqueId;
        this.LayoutSessionHandle = setTestDetailsOptions.LayoutSessionHandle;
        this.InitVariableValues();
        if (this.VariableValues.FindIndex((SetTestDetailsForm.VariableValueItemStub v) => !string.IsNullOrEmpty(v.ReplacementComponent)) > -1)
        {
          this.ComponentReplacing.Checked = true;
        }
        else
        {
          this.Variations.Class = "hide-test-component";
        }
        if (this.VariableValues.Count > 0)
        {
          this.ResetContainer.Visible = true;
        }
        List<SetTestDetailsForm.VariableValueItemStub> variableValues = this.VariableValues;
        if (this.VariableValues.Count == 0)
        {
          ID newID = ID.NewID;
          SetTestDetailsForm.VariableValueItemStub variableValueItemStub = new SetTestDetailsForm.VariableValueItemStub(newID, Translate.Text("Original"));
          if (this.Rendering != null && this.Rendering.Datasource != this.ContextItemUri.ItemID.ToString() && !string.IsNullOrEmpty(this.Rendering.Datasource))
          {
            variableValueItemStub.Datasource = this.Rendering.Datasource;
          }
          else
          {
            variableValueItemStub.Datasource = this.ContextItemUri.ItemID.ToString();
          }
          variableValues.Insert(0, variableValueItemStub);
          this.VariableValues = variableValues;
          string text = this.RenderVariableValue(variableValueItemStub);
          this.SetControlsState();
          SheerResponse.Insert(this.Variations.ClientID, "afterBegin", this.RenderVariableDatasourceValue(variableValueItemStub));
          SheerResponse.Insert(this.Variations.ClientID, "afterBegin", text);
          SheerResponse.Eval("Sitecore.CollapsiblePanel.newAdded('{0}')".FormatWith(new object[]
          {
                        newID.ToShortID()
          }));
        }
        if (this.Rendering != null)
        {
          Item item = TestingUtil.MultiVariateTesting.GetVariableItem(this.Rendering, Client.ContentDatabase);
          if (item != null && !item.Access.CanCreate())
          {
            this.NewVariation.Disabled =true;
          }
        }
        this.SetControlsState();
        this.Render();
      }
    }

    protected override void OnOK(object sender, EventArgs args)
    {
      DeviceDefinition deviceDefinition = this.Device;
      Assert.IsNotNull(deviceDefinition, "device");
      TestDefinitionItem testDefinitionItem = TestingUtil.MultiVariateTesting.GetTestDefinition(deviceDefinition, new ID(this.RenderingUniqueId), Client.ContentDatabase);
      if (testDefinitionItem == null)
      {
        ItemUri contextItemUri = this.ContextItemUri;
        if (contextItemUri == null)
        {
          return;
        }
        Item item = Client.ContentDatabase.GetItem(contextItemUri.ToDataUri());
        if (item != null)
        {
          testDefinitionItem = TestingUtil.MultiVariateTesting.AddTestDefinition(item);
        }
      }
      if (testDefinitionItem == null)
      {
        SheerResponse.Alert("The action cannot be executed because of security restrictions.", new string[0]);
      }
      else if (this.Rendering != null)
      {
        MultivariateTestVariableItem multivariateTestVariableItem = TestingUtil.MultiVariateTesting.GetVariableItem(this.Rendering, Client.ContentDatabase);
        if (multivariateTestVariableItem == null)
        {
          multivariateTestVariableItem = TestingUtil.MultiVariateTesting.AddVariable(testDefinitionItem, this.Rendering, Client.ContentDatabase);
        }
        List<ID> list;
        if (multivariateTestVariableItem == null)
        {
          SheerResponse.Alert("The action cannot be executed because of security restrictions.", new string[0]);
        }
        else if (!this.UpdateVariableValues(multivariateTestVariableItem, out list))
        {
          SheerResponse.Alert("The action cannot be executed because of security restrictions.", new string[0]);
        }
        else
        {
          string dialogResut = SetTestDetailsOptions.GetDialogResut(multivariateTestVariableItem.ID, list);
          SheerResponse.SetDialogValue(dialogResut);
          SheerResponse.CloseWindow();
        }
      }
    }

    [UsedImplicitly]
    protected void RemoveVariation(string variationId)
    {
      Assert.ArgumentNotNull(variationId, "variationId");
      ID id = ShortID.DecodeID(variationId);
      List<SetTestDetailsForm.VariableValueItemStub> variableValues = this.VariableValues;
      int num = variableValues.FindIndex((SetTestDetailsForm.VariableValueItemStub value) => value.Id == id);
      if (num < 0)
      {
        SheerResponse.Alert("Item not found.", new string[0]);
      }
      else
      {
        variableValues.RemoveAt(num);
        SheerResponse.Remove(variationId);
        this.VariableValues = variableValues;
        this.SetControlsState();
      }
    }

    [UsedImplicitly, HandleMessage("variation:rename")]
    protected void RenameVariation(Message message)
    {
      string text = message.Arguments["variationId"];
      string text2 = message.Arguments["name"];
      Assert.ArgumentNotNull(text, "variationId");
      Assert.ArgumentNotNull(text2, "name");
      ID id = ShortID.DecodeID(text);
      List<SetTestDetailsForm.VariableValueItemStub> variableValues = this.VariableValues;
      int num = variableValues.FindIndex((SetTestDetailsForm.VariableValueItemStub value) => value.Id == id);
      if (num < 0)
      {
        SheerResponse.Alert("Item not found.", new string[0]);
      }
      else if (string.IsNullOrEmpty(text2))
      {
        SheerResponse.Alert("An item name cannot be blank.", new string[0]);
        SheerResponse.Eval("Sitecore.CollapsiblePanel.editName(\"{0}\")".FormatWith(new object[]
        {
                    text
        }));
      }
      else
      {
        variableValues[num].Name = text2;
        this.VariableValues = variableValues;
      }
    }

    protected virtual void Render()
    {
      HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
      foreach (SetTestDetailsForm.VariableValueItemStub current in this.VariableValues)
      {
        htmlTextWriter.Write(this.RenderVariableValue(current));
      }
      string text = htmlTextWriter.InnerWriter.ToString();
      if (!string.IsNullOrEmpty(text))
      {
        this.Variations.InnerHtml = text;
      }
    }

    [HandleMessage("variation:reset", true)]
    protected void Reset_Click(ClientPipelineArgs args)
    {
      if (args.IsPostBack)
      {
        if (args.HasResult && !(args.Result == "no"))
        {
          RenderingDefinition renderingDefinition = this.Rendering;
          string dialogValue = "#reset#";
          if (renderingDefinition != null)
          {
            Item item = TestingUtil.MultiVariateTesting.GetVariableItem(renderingDefinition, Client.ContentDatabase);
            if (item == null)
            {
              SheerResponse.SetDialogValue(dialogValue);
              SheerResponse.CloseWindow();
              return;
            }
            if (!item.Access.CanDelete())
            {
              SheerResponse.Alert("The action cannot be executed because of security restrictions.", new string[0]);
              return;
            }
            Item parent = item.Parent;
            item.Delete();
            if (parent != null && parent.Access.CanDelete() && !parent.HasChildren)
            {
              parent.Delete();
            }
          }
          SheerResponse.SetDialogValue(dialogValue);
          SheerResponse.CloseWindow();
        }
      }
      else
      {
        SheerResponse.Confirm("Component will be removed from the test set. Are you sure you want to continue?");
        args.WaitForPostBack();
      }
    }

    protected void ResetVariationContent(string variationId)
    {
      Assert.ArgumentNotNull(variationId, "variationId");
      ID id = ShortID.DecodeID(variationId);
      List<SetTestDetailsForm.VariableValueItemStub> variableValues = this.VariableValues;
      SetTestDetailsForm.VariableValueItemStub variableValueItemStub = variableValues.Find((SetTestDetailsForm.VariableValueItemStub v) => v.Id == id);
      if (variableValueItemStub != null)
      {
        variableValueItemStub.Datasource = string.Empty;
        HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
        this.RenderContentControls(htmlTextWriter, variableValueItemStub);
        SheerResponse.SetOuterHtml(variationId + "_content", htmlTextWriter.InnerWriter.ToString());
        SheerResponse.SetInnerHtml(variationId + "_data_examples", string.Empty);
        this.VariableValues = variableValues;
      }
    }

    protected void ResetVariationComponent(string variationId)
    {
      Assert.ArgumentNotNull(variationId, "variationId");
      ID id = ShortID.DecodeID(variationId);
      List<SetTestDetailsForm.VariableValueItemStub> variableValues = this.VariableValues;
      SetTestDetailsForm.VariableValueItemStub variableValueItemStub = variableValues.Find((SetTestDetailsForm.VariableValueItemStub v) => v.Id == id);
      if (variableValueItemStub != null)
      {
        variableValueItemStub.ReplacementComponent = string.Empty;
        HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
        this.RenderComponentControls(htmlTextWriter, variableValueItemStub);
        SheerResponse.SetOuterHtml(variationId + "_component", htmlTextWriter.InnerWriter.ToString());
        this.VariableValues = variableValues;
      }
    }

    [HandleMessage("variation:setcomponent", true)]
    protected void SetComponent(ClientPipelineArgs args)
    {
      string text = args.Parameters["variationid"];
      if (string.IsNullOrEmpty(text))
      {
        SheerResponse.Alert("Item not found.", new string[0]);
      }
      else if (this.Rendering == null || this.Layout == null)
      {
        SheerResponse.Alert("An error ocurred.", new string[0]);
      }
      else if (!args.IsPostBack)
      {
        string placeholder = this.Rendering.Placeholder;
        Assert.IsNotNull(placeholder, "placeholder");
        string text2 = this.Layout.ToXml();
        GetPlaceholderRenderingsArgs getPlaceholderRenderingsArgs = new GetPlaceholderRenderingsArgs(placeholder, text2, Client.ContentDatabase, ID.Parse(this.DeviceId));
        getPlaceholderRenderingsArgs.OmitNonEditableRenderings =true;
        getPlaceholderRenderingsArgs.Options.ShowOpenProperties = false;
        CorePipeline.Run("getPlaceholderRenderings", getPlaceholderRenderingsArgs);
        string dialogURL = getPlaceholderRenderingsArgs.DialogURL;
        if (string.IsNullOrEmpty(dialogURL))
        {
          SheerResponse.Alert("An error ocurred.", new string[0]);
        }
        else
        {
          SheerResponse.ShowModalDialog(dialogURL, "720px", "470px", string.Empty, true);
          args.WaitForPostBack();
        }
      }
      else if (args.HasResult)
      {
        ID id = ShortID.DecodeID(text);
        List<SetTestDetailsForm.VariableValueItemStub> variableValues = this.VariableValues;
        SetTestDetailsForm.VariableValueItemStub variableValueItemStub = variableValues.Find((SetTestDetailsForm.VariableValueItemStub v) => v.Id == id);
        if (variableValueItemStub != null)
        {
          string replacementComponent;
          if (args.Result.IndexOf(',') >= 0)
          {
            string[] array = args.Result.Split(new char[]
            {
                            ','
            });
            replacementComponent = array[0];
          }
          else
          {
            replacementComponent = args.Result;
          }
          variableValueItemStub.ReplacementComponent = replacementComponent;
          HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
          this.RenderComponentControls(htmlTextWriter, variableValueItemStub);
          SheerResponse.SetOuterHtml(text + "_component", htmlTextWriter.InnerWriter.ToString());
          this.VariableValues = variableValues;
        }
      }
    }

    [HandleMessage("variation:setcontent", true)]
    protected void SetContent(ClientPipelineArgs args)
    {
      string text = args.Parameters["variationid"];
      if (string.IsNullOrEmpty(text))
      {
        SheerResponse.Alert("Item not found.", new string[0]);
      }
      else
      {
        ID id = ShortID.DecodeID(text);
        string replacementComponent = this.VariableValues.First((SetTestDetailsForm.VariableValueItemStub x) => x.Id.ToShortID() == id).ReplacementComponent;
        if (args.IsPostBack)
        {
          if (args.HasResult)
          {
            List<SetTestDetailsForm.VariableValueItemStub> variableValues = this.VariableValues;
            SetTestDetailsForm.VariableValueItemStub variableValueItemStub = variableValues.Find((SetTestDetailsForm.VariableValueItemStub v) => v.Id == id);
            if (variableValueItemStub != null)
            {
              variableValueItemStub.Datasource = args.Result;
              HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
              this.RenderContentControls(htmlTextWriter, variableValueItemStub);
              SheerResponse.SetOuterHtml(text + "_content", htmlTextWriter.InnerWriter.ToString());
              SheerResponse.SetOuterHtml(text + "_data_examples", this.RenderVariableDatasourceValue(variableValueItemStub));
              this.VariableValues = variableValues;
            }
          }
        }
        else
        {
          SetTestDetailsForm.VariableValueItemStub variableValueItemStub = this.VariableValues.Find((SetTestDetailsForm.VariableValueItemStub v) => v.Id == id);
          if (variableValueItemStub != null)
          {
            if (this.Rendering != null && !string.IsNullOrEmpty(this.Rendering.ItemID))
            {
              Item item = Client.ContentDatabase.GetItem(this.Rendering.ItemID);
              if (item == null)
              {
                SheerResponse.Alert("Item not found.", new string[0]);
              }
              else
              {
                Item item2 = (this.ContextItemUri == null) ? null : Client.ContentDatabase.GetItem(this.ContextItemUri.ToDataUri());
                if (replacementComponent != null && ID.IsID(replacementComponent))
                {
                  item = Client.ContentDatabase.GetItem(new ID(replacementComponent));
                }
                GetRenderingDatasourceArgs getRenderingDatasourceArgs = new GetRenderingDatasourceArgs(item);
                getRenderingDatasourceArgs.FallbackDatasourceRoots = new List<Item>
                                {
                                    Client.ContentDatabase.GetRootItem()
                                };
                getRenderingDatasourceArgs.ContentLanguage = (item2 != null) ? item2.Language : null;
                getRenderingDatasourceArgs.ContextItemPath = (item2 != null) ? item2.Paths.FullPath : string.Empty;
                getRenderingDatasourceArgs.ShowDialogIfDatasourceSetOnRenderingItem = true;
                getRenderingDatasourceArgs.CurrentDatasource = string.IsNullOrEmpty(variableValueItemStub.Datasource) ? this.Rendering.Datasource : variableValueItemStub.Datasource;
                GetRenderingDatasourceArgs getRenderingDatasourceArgs2 = getRenderingDatasourceArgs;
                CorePipeline.Run("getRenderingDatasource", getRenderingDatasourceArgs2);
                if (string.IsNullOrEmpty(getRenderingDatasourceArgs2.DialogUrl))
                {
                  SheerResponse.Alert("An error ocurred.", new string[0]);
                }
                else
                {
                  SheerResponse.ShowModalDialog(getRenderingDatasourceArgs2.DialogUrl, "1200px", "700px", string.Empty, true);
                  args.WaitForPostBack();
                }
              }
            }
          }
        }
      }
    }

    protected void ShowConfirm(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (args.IsPostBack)
      {
        if (args.HasResult && args.Result != "no")
        {
          SheerResponse.Eval("scToggleTestComponentSection()");
          List<SetTestDetailsForm.VariableValueItemStub> variableValues = this.VariableValues;
          foreach (SetTestDetailsForm.VariableValueItemStub current in variableValues)
          {
            if (!string.IsNullOrEmpty(current.ReplacementComponent))
            {
              current.ReplacementComponent = string.Empty;
              using (HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter()))
              {
                this.RenderComponentControls(htmlTextWriter, current);
                SheerResponse.SetOuterHtml(current.Id.ToShortID() + "_component", htmlTextWriter.InnerWriter.ToString());
              }
            }
          }
          this.VariableValues = variableValues;
        }
        else
        {
          this.ComponentReplacing.Checked = true;
        }
      }
      else
      {
        SheerResponse.Confirm("Test component settings will be removed. Are you sure you want to continue?");
        args.WaitForPostBack();
      }
    }

    protected virtual bool UpdateVariableValues(MultivariateTestVariableItem variableItem, out List<ID> modifiedVariations)
    {
      Assert.ArgumentNotNull(variableItem, "variableItem");
      modifiedVariations = new List<ID>();
      List<SetTestDetailsForm.VariableValueItemStub> variableValues = this.VariableValues;
      IEnumerable<MultivariateTestValueItem> variableValues2 = TestingUtil.MultiVariateTesting.GetVariableValues(variableItem);
      List<MultivariateTestValueItem> list = new List<MultivariateTestValueItem>(variableValues2);
      DefaultComparer comparer = new DefaultComparer();
      list.Sort((MultivariateTestValueItem lhs, MultivariateTestValueItem rhs) => comparer.Compare(lhs, rhs));
      int num = (list.Count > 0) ? (list[0].InnerItem.Appearance.Sortorder - 1) : Settings.DefaultSortOrder;
      TemplateID templateID = new TemplateID(MultivariateTestValueItem.TemplateID);
      List<KeyValuePair<MultivariateTestValueItem, SetTestDetailsForm.VariableValueItemStub>> list2 = new List<KeyValuePair<MultivariateTestValueItem, SetTestDetailsForm.VariableValueItemStub>>();
      List<KeyValuePair<int, SetTestDetailsForm.VariableValueItemStub>> list3 = new List<KeyValuePair<int, SetTestDetailsForm.VariableValueItemStub>>();
      for (int i = variableValues.Count - 1; i >= 0; i--)
      {
        SetTestDetailsForm.VariableValueItemStub variableValueItemStub = variableValues[i];
        ID currentId = variableValueItemStub.Id;
        int num2 = list.FindIndex((MultivariateTestValueItem item) => item.ID == currentId);
        if (num2 < 0)
        {
          KeyValuePair<int, SetTestDetailsForm.VariableValueItemStub> current = new KeyValuePair<int, SetTestDetailsForm.VariableValueItemStub>(num--, variableValueItemStub);
          list3.Add(current);
        }
        else
        {
          MultivariateTestValueItem multivariateTestValueItem = list[num2];
          if (SetTestDetailsForm.IsVariableValueChanged(multivariateTestValueItem, variableValueItemStub))
          {
            list2.Add(new KeyValuePair<MultivariateTestValueItem, SetTestDetailsForm.VariableValueItemStub>(multivariateTestValueItem, variableValueItemStub));
          }
          list.RemoveAt(num2);
        }
      }
      bool arg_190_0;
      if (list.Count != 0)
      {
        arg_190_0 = !list.Exists((MultivariateTestValueItem item) => !item.InnerItem.Access.CanDelete());
      }
      else
      {
        arg_190_0 = true;
      }
      bool flag = arg_190_0;
      bool flag2 = list3.Count == 0 || variableItem.InnerItem.Access.CanAdd(templateID);
      bool arg_1E9_0;
      if (list2.Count != 0)
      {
        arg_1E9_0 = !list2.Exists((KeyValuePair<MultivariateTestValueItem, SetTestDetailsForm.VariableValueItemStub> p) => !SetTestDetailsForm.CanUpdateItem(p.Key));
      }
      else
      {
        arg_1E9_0 = true;
      }
      bool flag3 = arg_1E9_0;
      bool flag4 = flag2 && flag3 && flag;
      bool result;
      if (flag4)
      {
        using (List<MultivariateTestValueItem>.Enumerator enumerator = list.GetEnumerator())
        {
          while (enumerator.MoveNext())
          {
            Item item4 = enumerator.Current;
            modifiedVariations.Add(item4.ID);
            item4.Delete();
          }
        }
        foreach (KeyValuePair<int, SetTestDetailsForm.VariableValueItemStub> current in list3)
        {
          SetTestDetailsForm.VariableValueItemStub variableValueItemStub = current.Value;
          int key = current.Key;
          string text = variableValueItemStub.Name;
          if (ItemUtil.ContainsNonASCIISymbols(text))
          {
            Item item2 = variableItem.Database.GetItem(templateID.ID);
            text = ((item2 != null) ? item2.Name : "Unnamed item");
          }
          if (!ItemUtil.IsItemNameValid(text))
          {
            try
            {
              text = ItemUtil.ProposeValidItemName(text);
            }
            catch (Exception)
            {
              result = false;
              return result;
            }
          }
          text = ItemUtil.GetUniqueName(variableItem, text);
          Item item3 = variableItem.InnerItem.Add(text, templateID);
          Assert.IsNotNull(item3, "newVariableValue");
          SetTestDetailsForm.UpdateVariableValueItem((MultivariateTestValueItem)item3, variableValueItemStub, key);
        }
        foreach (KeyValuePair<MultivariateTestValueItem, SetTestDetailsForm.VariableValueItemStub> current2 in list2)
        {
          MultivariateTestValueItem key2 = current2.Key;
          SetTestDetailsForm.VariableValueItemStub value = current2.Value;
          modifiedVariations.Add(key2.ID);
          SetTestDetailsForm.UpdateVariableValueItem(key2, value);
        }
      }
      result = flag4;
      return result;
    }

    private static bool CanUpdateItem(Item item)
    {
      return (Context.IsAdministrator || !item.Locking.IsLocked() || item.Locking.HasLock()) && !item.Appearance.ReadOnly && item.Access.CanWrite();
    }

    private static bool IsVariableValueChanged(MultivariateTestValueItem variableItem, SetTestDetailsForm.VariableValueItemStub variableStub)
    {
      Assert.ArgumentNotNull(variableItem, "variableItem");
      bool result;
      if (variableItem.Name != variableStub.Name)
      {
        result = true;
      }
      else if (variableItem.Datasource.Uri == null && !string.IsNullOrEmpty(variableStub.Datasource))
      {
        result = true;
      }
      else
      {
        ID @null = ID.Null;
        ID.TryParse(variableStub.Datasource, out @null);
        result = ((variableItem.Datasource.Uri != null && variableItem.Datasource.Uri.ItemID != @null) || (variableItem.ReplacementComponent.Uri != null && variableItem.Database.GetItem(variableItem.ReplacementComponent.Uri).Paths.FullPath != variableStub.ReplacementComponent) || variableItem.HideComponent != variableStub.HideComponent);
      }
      return result;
    }

    private static void UpdateVariableValueItem(MultivariateTestValueItem variableValue, SetTestDetailsForm.VariableValueItemStub variableStub)
    {
      Assert.ArgumentNotNull(variableValue, "variableValue");
      SetTestDetailsForm.UpdateVariableValueItem(variableValue, variableStub, variableValue.InnerItem.Appearance.Sortorder);
    }

    private static void UpdateVariableValueItem(MultivariateTestValueItem variableValue, SetTestDetailsForm.VariableValueItemStub variableStub, int sortOrder)
    {
      Assert.ArgumentNotNull(variableValue, "variableValue");
      using (new EditContext(variableValue))
      {
        variableValue["Name"] =  variableStub.Name;
        variableValue.Datasource.Value = variableStub.Datasource;
        variableValue.HideComponent = variableStub.HideComponent;
        variableValue.ReplacementComponent.Value = variableStub.ReplacementComponent;
        variableValue.InnerItem.Appearance.Sortorder = sortOrder;
      }
    }

    private Menu GetActionsMenu(string id)
    {
      Assert.IsNotNullOrEmpty(id, "id");
      Menu menu = new Menu();
      menu.ID = id + "_menu";
      string text = Images.GetThemedImageSource("Office/16x16/delete.png");
      string text2 = "RemoveVariation(\\\"{0}\\\")".FormatWith(new object[]
      {
                id
      });
      menu.Add("Delete", text, text2);
      text = string.Empty;
      text2 = "javascript:Sitecore.CollapsiblePanel.rename(this, event, \"{0}\")".FormatWith(new object[]
      {
                id
      });
      menu.Add("Rename", text, text2);
      return menu;
    }

    private Item GetCurrentContent(SetTestDetailsForm.VariableValueItemStub value, out bool isFallback)
    {
      Assert.ArgumentNotNull(value, "value");
      isFallback = false;
      Item item = null;
      Item result;
      if (!string.IsNullOrEmpty(value.Datasource))
      {
        result = Client.ContentDatabase.GetItem(value.Datasource);
      }
      else
      {
        if (this.Rendering != null && !string.IsNullOrEmpty(this.Rendering.Datasource))
        {
          item = Client.ContentDatabase.GetItem(this.Rendering.Datasource);
          isFallback = true;
        }
        result = item;
      }
      return result;
    }

    private void RenderContentControls(HtmlTextWriter output, SetTestDetailsForm.VariableValueItemStub value)
    {
      Assert.ArgumentNotNull(output, "output");
      Assert.ArgumentNotNull(value, "value");
      ShortID shortID = value.Id.ToShortID();
      bool flag;
      Item currentContent = this.GetCurrentContent(value, out flag);
      string text = flag ? "default-values" : string.Empty;
      if (value.HideComponent)
      {
        text += " display-off";
      }
      output.Write("<div {0} id='{1}_content'>", (text == string.Empty) ? text : ("class='" + text + "'"), shortID);
      string text2 = value.HideComponent ? "javascript:void(0);" : ("variation:setcontent(variationid=" + shortID + ")");
      string text3 = value.HideComponent ? "javascript:void(0);" : "ResetVariationContent(\\\"{0}\\\")".FormatWith(new object[]
      {
                shortID
      });
      if (currentContent == null)
      {
        this.RenderPicker(output, value.Datasource, text2, text3, true);
      }
      else
      {
        this.RenderPicker(output, currentContent, text2, text3, true);
      }
      output.Write("</div>");
    }

    protected void SelectSearchResult(string resultid, string variationId)
    {
      List<SetTestDetailsForm.VariableValueItemStub> variableValues = this.VariableValues;
      SetTestDetailsForm.VariableValueItemStub variableValueItemStub = variableValues.Find((SetTestDetailsForm.VariableValueItemStub v) => v.Id == new ID(variationId));
      if (variableValueItemStub != null)
      {
        variableValueItemStub.Datasource = new ID(resultid).ToString();
        HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
        this.RenderContentControls(htmlTextWriter, variableValueItemStub);
        SheerResponse.SetOuterHtml(variationId + "_content", htmlTextWriter.InnerWriter.ToString());
        SheerResponse.SetOuterHtml(variationId + "_data_examples", this.RenderVariableDatasourceValue(variableValueItemStub));
        this.VariableValues = variableValues;
      }
    }

    private void RenderDisplayControls(HtmlTextWriter output, SetTestDetailsForm.VariableValueItemStub value)
    {
      Assert.ArgumentNotNull(output, "output");
      Assert.ArgumentNotNull(value, "value");
      ShortID arg = value.Id.ToShortID();
      output.Write("<input type='checkbox' onfocus='this.blur();' onclick=\"javascript:return scSwitchRendering(this, event, '{0}')\" ", arg);
      if (value.HideComponent)
      {
        output.Write(" checked='checked' ");
      }
      output.Write("/>");
      output.Write("<span class='display-component-title'>");
      output.Write(Translate.Text("Hide Component"));
      output.Write("</span>");
    }

    private void RenderPicker(HtmlTextWriter writer, Item item, string click, string reset, bool prependEllipsis)
    {
      Assert.ArgumentNotNull(writer, "writer");
      Assert.ArgumentNotNull(click, "click");
      Assert.ArgumentNotNull(reset, "reset");
      string themedImageSource = Images.GetThemedImageSource((item != null) ? item.Appearance.Icon : string.Empty, ImageDimension.id16x16);
      string text = Translate.Text("[Not set]");
      string text2 = "item-picker";
      if (item != null)
      {
        text = (prependEllipsis ? ".../" : string.Empty);
        text += item.DisplayName;
      }
      else
      {
        text2 += " not-set";
      }
      writer.Write(string.Format("<div style=\"background-image:url('{0}')\" class='{1}'>", themedImageSource, text2));
      writer.Write("<a href='#' class='pick-button' onclick=\"{0}\" title=\"{1}\">...</a>", Context.ClientPage.GetClientEvent(click), Translate.Text("Select"));
      writer.Write("<a href='#' class='reset-button' onclick=\"{0}\" title=\"{1}\"></a>", Context.ClientPage.GetClientEvent(reset), Translate.Text("Reset"));
      writer.Write("<span title=\"{0}\">{1}</span>", (item == null) ? string.Empty : item.DisplayName, text);
      writer.Write("</div>");
    }

    private void RenderPicker(HtmlTextWriter writer, string datasource, string clickCommand, string resetCommand, bool prependEllipsis)
    {
      Assert.ArgumentNotNull(writer, "writer");
      Assert.ArgumentNotNull(clickCommand, "clickCommand");
      Assert.ArgumentNotNull(resetCommand, "resetCommand");
      string text = Translate.Text("[Not set]");
      string text2 = "item-picker";
      if (!String.IsNullOrEmpty(datasource))
      {
        text = datasource;
      }
      else
      {
        text2 += " not-set";
      }
      writer.Write(string.Format("<div class='{0}'>", text2));
      writer.Write("<a href='#' class='pick-button' onclick=\"{0}\" title=\"{1}\">...</a>", Context.ClientPage.GetClientEvent(clickCommand), Translate.Text("Select"));
      writer.Write("<a href='#' class='reset-button' onclick=\"{0}\" title=\"{1}\"></a>", Context.ClientPage.GetClientEvent(resetCommand), Translate.Text("Reset"));
      string text3 = text;
      if (text3 != null)
      {
        if (text3.Length > 30)
        {
          text3 = text3.Substring(0, 29) + "...";
        }
      }
      writer.Write("<span title=\"{0}\">{1}</span>", text, text3);
      writer.Write("</div>");
    }

    private string RenderVariableValue(SetTestDetailsForm.VariableValueItemStub value)
    {
      CollapsiblePanelRenderer collapsiblePanelRenderer = new CollapsiblePanelRenderer();
      CollapsiblePanelRenderer.ActionsContext actionsContext = new CollapsiblePanelRenderer.ActionsContext
      {
        IsVisible = true
      };
      string text = value.Id.ToShortID().ToString();
      actionsContext.Menu = this.GetActionsMenu(text);
      CollapsiblePanelRenderer.NameContext nameContext = new CollapsiblePanelRenderer.NameContext(value.Name)
      {
        OnNameChanged = "javascript:Sitecore.CollapsiblePanel.nameChanged(this, event)"
      };
      string text2 = this.RenderVariableValueDetails(value);
      text2 += this.RenderVariableDatasourceValue(value);
      return collapsiblePanelRenderer.Render(text, text2, nameContext, actionsContext);
    }

    private string RenderVariableDatasourceValue(SetTestDetailsForm.VariableValueItemStub value)
    {
      HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
      htmlTextWriter.Write("<div id=" + value.Id.ToShortID().ToString() + "_data_examples class='something'>");
      DatasourceExamplesRenderer datasourceExamplesRenderer = new DatasourceExamplesRenderer();
      string value2 = datasourceExamplesRenderer.Render(value.Datasource, Context.ContentDatabase, 10);
      htmlTextWriter.Write(value2);
      htmlTextWriter.Write("</div>");
      return htmlTextWriter.InnerWriter.ToString();
    }

    private string RenderVariableValueDetails(SetTestDetailsForm.VariableValueItemStub value)
    {
      HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
      htmlTextWriter.Write("<table class='top-row'>");
      htmlTextWriter.Write("<tr>");
      htmlTextWriter.Write("<td class='left test-title'>");
      htmlTextWriter.Write(Translate.Text("Test Content Item:"));
      htmlTextWriter.Write("</td>");
      htmlTextWriter.Write("<td class='right'>");
      htmlTextWriter.Write("</td>");
      htmlTextWriter.Write("</tr>");
      htmlTextWriter.Write("<tr>");
      htmlTextWriter.Write("<td class='left test-content'>");
      this.RenderContentControls(htmlTextWriter, value);
      htmlTextWriter.Write("</td>");
      htmlTextWriter.Write("<td class='right display-component'>");
      this.RenderDisplayControls(htmlTextWriter, value);
      htmlTextWriter.Write("</td>");
      htmlTextWriter.Write("</tr>");
      htmlTextWriter.Write("<tr class='component-row'>");
      htmlTextWriter.Write("<td class='left test-title'>");
      htmlTextWriter.Write(Translate.Text("Test Component Design:"));
      htmlTextWriter.Write("</td>");
      htmlTextWriter.Write("<td rowspan='2' class='right'>");
      htmlTextWriter.Write("</td>");
      htmlTextWriter.Write("</tr>");
      htmlTextWriter.Write("<tr class='component-row'>");
      htmlTextWriter.Write("<td class='left test-component'>");
      this.RenderComponentControls(htmlTextWriter, value);
      htmlTextWriter.Write("</td>");
      htmlTextWriter.Write("</tr>");
      htmlTextWriter.Write("</table>");
      return htmlTextWriter.InnerWriter.ToString();
    }

    private void RenderComponentControls(HtmlTextWriter output, SetTestDetailsForm.VariableValueItemStub value)
    {
      ShortID shortID = value.Id.ToShortID();
      bool flag;
      Item currentRenderingItem = this.GetCurrentRenderingItem(value, out flag);
      string thumbnailSrc = SetTestDetailsForm.GetThumbnailSrc(currentRenderingItem);
      string text = flag ? "default-values" : string.Empty;
      if (value.HideComponent)
      {
        text += " display-off";
      }
      output.Write("<div id='{0}_component' {1}>", shortID, string.IsNullOrEmpty(text) ? string.Empty : ("class='" + text + "'"));
      output.Write("<div style=\"background-image:url('{0}')\" class='thumbnail-container'>", thumbnailSrc);
      output.Write("</div>");
      output.Write("<div class='picker-container'>");
      string click = value.HideComponent ? "javascript:void(0);" : ("variation:setcomponent(variationid=" + shortID + ")");
      string reset = value.HideComponent ? "javascript:void(0);" : "ResetVariationComponent(\\\"{0}\\\")".FormatWith(new object[]
      {
                shortID
      });
      this.RenderPicker(output, currentRenderingItem, click, reset, false);
      output.Write("</div>");
      output.Write("</div>");
    }

    private static string GetThumbnailSrc(Item item)
    {
      string text = "/sitecore/shell/blank.gif";
      string result;
      if (item == null)
      {
        result = text;
      }
      else
      {
        if (!string.IsNullOrEmpty(item.Appearance.Thumbnail) && item.Appearance.Thumbnail != Settings.DefaultThumbnail)
        {
          string thumbnailSrc = UIUtil.GetThumbnailSrc(item, 128, 128);
          if (!string.IsNullOrEmpty(thumbnailSrc))
          {
            text = thumbnailSrc;
          }
        }
        else
        {
          text = Images.GetThemedImageSource(item.Appearance.Icon, ImageDimension.id48x48);
        }
        result = text;
      }
      return result;
    }

    private Item GetCurrentRenderingItem(SetTestDetailsForm.VariableValueItemStub value, out bool isFallback)
    {
      isFallback = false;
      Item result;
      if (!string.IsNullOrEmpty(value.ReplacementComponent))
      {
        result = Client.ContentDatabase.GetItem(value.ReplacementComponent);
      }
      else
      {
        RenderingDefinition renderingDefinition = this.Rendering;
        if (renderingDefinition == null)
        {
          result = null;
        }
        else if (!string.IsNullOrEmpty(renderingDefinition.ItemID))
        {
          isFallback = true;
          result = Client.ContentDatabase.GetItem(renderingDefinition.ItemID);
        }
        else
        {
          result = null;
        }
      }
      return result;
    }

    private void SetControlsState()
    {
      int count = this.VariableValues.Count;
      this.OK.Disabled = count < 2;
      this.NoVariations.Visible = count < 1;
      this.NewVariation.Disabled = count > 256;
    }
  }
}
