using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NetStudio.Common;
using NetStudio.Common.IndusCom;
using NetStudio.Common.Manager;
using NetStudio.IPS.Local;
using NetStudio.Mitsubishi.Models;

namespace NetStudio.IPS;

public class MitsubishiSerialDevice : Form
{
	private Channel channel;

	private Device? device;

	public EventDeviceChanged? OnDeviceChanged;

	private IContainer components;

	private Button btnCancel;

	private Button btnSave;

	private ErrorProvider errorProvider1;

	private ComboBox cboxDeviceType;

	private Label label4;

	private GroupBox groupBox1;

	private TextBox txtDescription;

	private GroupBox gbGeneral;

	private NumericUpDown txtConnectRetries;

	private Label label6;

	private TextBox txtDeviceName;

	private Label label1;

	private CheckBox chkAutoReconnect;

	private TextBox txtChannel;

	private CheckBox chkActive;

	private Label label2;

	private NumericUpDown txtStationNo;

	private Label label3;

	public MitsubishiSerialDevice(Channel channel, Device? device = null)
	{
		InitializeComponent();
		try
		{
			this.channel = channel;
			txtChannel.Text = channel.Name;
			if (device != null)
			{
				Text = "Edit: Device";
				this.device = (Device)device.Clone();
			}
			else
			{
				Text = "Add new: Device";
				this.device.BlockSize = this.device.GetBlockSize(this.channel.Protocol);
			}
			this.device.ChannelId = channel.Id;
			txtDeviceName.DataBindings.Add("Text", this.device, "Name", formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged);
			txtStationNo.DataBindings.Add("Value", this.device, "StationNo", formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged);
			chkActive.DataBindings.Add("Checked", this.device, "Active", formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged);
			chkAutoReconnect.DataBindings.Add("Checked", this.device, "AutoReconnect", formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged);
			txtConnectRetries.DataBindings.Add("Value", this.device, "ConnectRetries", formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged);
			txtDescription.DataBindings.Add("Text", this.device, "Description", formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged);
			new Binding("Enabled", this.channel, "Protocol", formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged).Format += delegate(object? sender, ConvertEventArgs e)
			{
				if (e.Value != null)
				{
					IpsProtocolType ipsProtocolType = (IpsProtocolType)e.Value;
					e.Value = ipsProtocolType == IpsProtocolType.MODBUS_TCP || ipsProtocolType == IpsProtocolType.MODBUS_RTU || ipsProtocolType == IpsProtocolType.MODBUS_ASCII;
				}
			};
			cboxDeviceType.DisplayMember = "Value";
			cboxDeviceType.ValueMember = "Key";
			Binding binding = new Binding("SelectedValue", this.device, "DeviceType", formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged);
			binding.Format += delegate(object? sender, ConvertEventArgs e)
			{
				if (e.Value != null)
				{
					e.Value = (CPUType)e.Value;
				}
			};
			cboxDeviceType.DataBindings.Add(binding);
			cboxDeviceType.Validating += CboxDeviceType_Validating;
			List<KeyValuePair<CPUType, string>> list = Extensions.GetDictionary<CPUType>().ToList();
			if (list != null && list.Count > 0)
			{
				list.RemoveAt(0);
			}
			cboxDeviceType.DataSource = list;
			txtDeviceName.KeyDown += OnTextBoxKeyDown;
			txtDescription.KeyDown += OnTextBoxKeyDown;
			txtDeviceName.KeyDown += OnTextBoxKeyDown;
			txtStationNo.KeyDown += OnTextBoxKeyDown;
			txtDescription.KeyDown += OnTextBoxKeyDown;
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void CboxDeviceType_Validating(object? sender, CancelEventArgs e)
	{
		ComboBox comboBox = (ComboBox)sender;
		errorProvider1.SetError(comboBox, null);
		if (comboBox.SelectedValue == null)
		{
			e.Cancel = true;
			comboBox.Focus();
			errorProvider1.SetIconAlignment(comboBox, ErrorIconAlignment.MiddleLeft);
			errorProvider1.SetIconPadding(comboBox, 5);
			errorProvider1.SetError(comboBox, "Please choose your CPU type");
		}
		else
		{
			e.Cancel = false;
			errorProvider1.SetError(comboBox, null);
			errorProvider1.Clear();
		}
	}

	private void FormDevice_Load(object sender, EventArgs e)
	{
		try
		{
			base.ActiveControl = txtDeviceName;
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void btnSave_Click(object? sender, EventArgs e)
	{
		try
		{
			if (device == null)
			{
				throw new Exception("Device is null");
			}
			if (string.IsNullOrEmpty(cboxDeviceType.Text) || string.IsNullOrWhiteSpace(cboxDeviceType.Text))
			{
				errorProvider1.SetIconAlignment(cboxDeviceType, ErrorIconAlignment.MiddleLeft);
				errorProvider1.SetIconPadding(cboxDeviceType, 4);
				errorProvider1.SetError(cboxDeviceType, "Please select your CPU type.");
			}
			if (errorProvider1.HasErrors)
			{
				return;
			}
			device.BlockSize = device.GetBlockSize(channel.Protocol);
			if (Text.StartsWith("add", StringComparison.OrdinalIgnoreCase))
			{
				EditHelper.AddDevice(device);
				if (OnDeviceChanged != null)
				{
					OnDeviceChanged(device, isAddnew: true);
				}
			}
			else
			{
				EditHelper.EditDevice(device);
				if (OnDeviceChanged != null)
				{
					OnDeviceChanged(device, isAddnew: false);
				}
			}
			base.DialogResult = DialogResult.OK;
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Return)
		{
			btnSave_Click(sender, e);
		}
	}

	private void txtDeviceName_Validating(object sender, CancelEventArgs e)
	{
		try
		{
			TextBox textBox = (TextBox)sender;
			if (!string.IsNullOrEmpty(textBox.Text) && !string.IsNullOrWhiteSpace(textBox.Text))
			{
				e.Cancel = false;
				errorProvider1.SetError(textBox, null);
				errorProvider1.Clear();
			}
			else
			{
				e.Cancel = true;
				textBox.Focus();
				errorProvider1.SetIconAlignment(txtDeviceName, ErrorIconAlignment.MiddleLeft);
				errorProvider1.SetIconPadding(txtDeviceName, 2);
				errorProvider1.SetError(textBox, "Please enter your device name");
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	protected override bool ProcessDialogKey(Keys keyData)
	{
		if (Control.ModifierKeys == Keys.None && keyData == Keys.Escape)
		{
			Close();
			return true;
		}
		return base.ProcessDialogKey(keyData);
	}

	private void cboxDeviceType_SelectedIndexChanged(object sender, EventArgs e)
	{
		try
		{
			ComboBox comboBox = (ComboBox)sender;
			if (device == null)
			{
				return;
			}
			switch (channel.Protocol)
			{
			case IpsProtocolType.FX_SERIAL_PROTOCOL:
				switch (comboBox.SelectedIndex)
				{
				default:
					device.BlockSize = 120;
					break;
				case 0:
					device.BlockSize = 64;
					break;
				}
				break;
			case IpsProtocolType.DEDICATED1_PROTOCOL:
			case IpsProtocolType.DEDICATED4_PROTOCOL:
			{
				int selectedIndex = comboBox.SelectedIndex;
				if (selectedIndex != 0 && (uint)(selectedIndex - 1) <= 1u)
				{
					device.BlockSize = 32;
				}
				else
				{
					device.BlockSize = 13;
				}
				break;
			}
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NetStudio.IPS.MitsubishiSerialDevice));
		this.btnCancel = new System.Windows.Forms.Button();
		this.btnSave = new System.Windows.Forms.Button();
		this.errorProvider1 = new System.Windows.Forms.ErrorProvider(this.components);
		this.cboxDeviceType = new System.Windows.Forms.ComboBox();
		this.label4 = new System.Windows.Forms.Label();
		this.groupBox1 = new System.Windows.Forms.GroupBox();
		this.txtDescription = new System.Windows.Forms.TextBox();
		this.gbGeneral = new System.Windows.Forms.GroupBox();
		this.txtConnectRetries = new System.Windows.Forms.NumericUpDown();
		this.label6 = new System.Windows.Forms.Label();
		this.txtDeviceName = new System.Windows.Forms.TextBox();
		this.label1 = new System.Windows.Forms.Label();
		this.chkAutoReconnect = new System.Windows.Forms.CheckBox();
		this.txtChannel = new System.Windows.Forms.TextBox();
		this.chkActive = new System.Windows.Forms.CheckBox();
		this.label2 = new System.Windows.Forms.Label();
		this.txtStationNo = new System.Windows.Forms.NumericUpDown();
		this.label3 = new System.Windows.Forms.Label();
		((System.ComponentModel.ISupportInitialize)this.errorProvider1).BeginInit();
		this.groupBox1.SuspendLayout();
		this.gbGeneral.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.txtConnectRetries).BeginInit();
		((System.ComponentModel.ISupportInitialize)this.txtStationNo).BeginInit();
		base.SuspendLayout();
		this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		this.btnCancel.Location = new System.Drawing.Point(320, 339);
		this.btnCancel.Name = "btnCancel";
		this.btnCancel.Size = new System.Drawing.Size(76, 30);
		this.btnCancel.TabIndex = 8;
		this.btnCancel.Text = "Cancel";
		this.btnCancel.UseVisualStyleBackColor = true;
		this.btnSave.Location = new System.Drawing.Point(238, 339);
		this.btnSave.Name = "btnSave";
		this.btnSave.Size = new System.Drawing.Size(76, 30);
		this.btnSave.TabIndex = 7;
		this.btnSave.Text = "Save";
		this.btnSave.UseVisualStyleBackColor = true;
		this.btnSave.Click += new System.EventHandler(btnSave_Click);
		this.errorProvider1.ContainerControl = this;
		this.cboxDeviceType.FormattingEnabled = true;
		this.cboxDeviceType.Location = new System.Drawing.Point(111, 85);
		this.cboxDeviceType.Name = "cboxDeviceType";
		this.cboxDeviceType.Size = new System.Drawing.Size(267, 23);
		this.cboxDeviceType.TabIndex = 44;
		this.cboxDeviceType.SelectedIndexChanged += new System.EventHandler(cboxDeviceType_SelectedIndexChanged);
		this.cboxDeviceType.Validating += new System.ComponentModel.CancelEventHandler(CboxDeviceType_Validating);
		this.label4.AutoSize = true;
		this.label4.ForeColor = System.Drawing.SystemColors.ControlText;
		this.label4.Location = new System.Drawing.Point(4, 87);
		this.label4.Name = "label4";
		this.label4.Size = new System.Drawing.Size(40, 15);
		this.label4.TabIndex = 43;
		this.label4.Text = "Series:";
		this.groupBox1.Controls.Add(this.txtDescription);
		this.groupBox1.ForeColor = System.Drawing.Color.Navy;
		this.groupBox1.Location = new System.Drawing.Point(8, 188);
		this.groupBox1.Name = "groupBox1";
		this.groupBox1.Size = new System.Drawing.Size(389, 145);
		this.groupBox1.TabIndex = 50;
		this.groupBox1.TabStop = false;
		this.groupBox1.Text = "Description";
		this.txtDescription.Location = new System.Drawing.Point(10, 22);
		this.txtDescription.Multiline = true;
		this.txtDescription.Name = "txtDescription";
		this.txtDescription.Size = new System.Drawing.Size(368, 110);
		this.txtDescription.TabIndex = 6;
		this.gbGeneral.Controls.Add(this.txtConnectRetries);
		this.gbGeneral.Controls.Add(this.label6);
		this.gbGeneral.Controls.Add(this.label4);
		this.gbGeneral.Controls.Add(this.cboxDeviceType);
		this.gbGeneral.Controls.Add(this.txtDeviceName);
		this.gbGeneral.Controls.Add(this.label1);
		this.gbGeneral.Controls.Add(this.chkAutoReconnect);
		this.gbGeneral.Controls.Add(this.txtChannel);
		this.gbGeneral.Controls.Add(this.chkActive);
		this.gbGeneral.Controls.Add(this.label2);
		this.gbGeneral.Controls.Add(this.txtStationNo);
		this.gbGeneral.Controls.Add(this.label3);
		this.gbGeneral.ForeColor = System.Drawing.Color.Navy;
		this.gbGeneral.Location = new System.Drawing.Point(8, 3);
		this.gbGeneral.Name = "gbGeneral";
		this.gbGeneral.Size = new System.Drawing.Size(389, 179);
		this.gbGeneral.TabIndex = 49;
		this.gbGeneral.TabStop = false;
		this.gbGeneral.Text = "General";
		this.txtConnectRetries.Location = new System.Drawing.Point(312, 119);
		this.txtConnectRetries.Maximum = new decimal(new int[4] { 3600, 0, 0, 0 });
		this.txtConnectRetries.Minimum = new decimal(new int[4] { 1, 0, 0, 0 });
		this.txtConnectRetries.Name = "txtConnectRetries";
		this.txtConnectRetries.Size = new System.Drawing.Size(66, 23);
		this.txtConnectRetries.TabIndex = 47;
		this.txtConnectRetries.Value = new decimal(new int[4] { 3, 0, 0, 0 });
		this.label6.AutoSize = true;
		this.label6.ForeColor = System.Drawing.SystemColors.ControlText;
		this.label6.Location = new System.Drawing.Point(213, 123);
		this.label6.Name = "label6";
		this.label6.Size = new System.Drawing.Size(96, 15);
		this.label6.TabIndex = 48;
		this.label6.Text = "Connect retries:";
		this.txtDeviceName.Location = new System.Drawing.Point(111, 51);
		this.txtDeviceName.Name = "txtDeviceName";
		this.txtDeviceName.Size = new System.Drawing.Size(267, 23);
		this.txtDeviceName.TabIndex = 1;
		this.label1.AutoSize = true;
		this.label1.ForeColor = System.Drawing.SystemColors.ControlText;
		this.label1.Location = new System.Drawing.Point(4, 21);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(87, 15);
		this.label1.TabIndex = 0;
		this.label1.Text = "Channel name:";
		this.chkAutoReconnect.AutoSize = true;
		this.chkAutoReconnect.ForeColor = System.Drawing.SystemColors.ControlText;
		this.chkAutoReconnect.Location = new System.Drawing.Point(213, 153);
		this.chkAutoReconnect.Name = "chkAutoReconnect";
		this.chkAutoReconnect.Size = new System.Drawing.Size(108, 19);
		this.chkAutoReconnect.TabIndex = 41;
		this.chkAutoReconnect.Text = "Auto reconnect";
		this.chkAutoReconnect.UseVisualStyleBackColor = true;
		this.txtChannel.Location = new System.Drawing.Point(111, 18);
		this.txtChannel.Name = "txtChannel";
		this.txtChannel.ReadOnly = true;
		this.txtChannel.Size = new System.Drawing.Size(267, 23);
		this.txtChannel.TabIndex = 9;
		this.chkActive.AutoSize = true;
		this.chkActive.ForeColor = System.Drawing.SystemColors.ControlText;
		this.chkActive.Location = new System.Drawing.Point(111, 153);
		this.chkActive.Name = "chkActive";
		this.chkActive.Size = new System.Drawing.Size(59, 19);
		this.chkActive.TabIndex = 3;
		this.chkActive.Text = "Active";
		this.chkActive.UseVisualStyleBackColor = true;
		this.label2.AutoSize = true;
		this.label2.ForeColor = System.Drawing.SystemColors.ControlText;
		this.label2.Location = new System.Drawing.Point(4, 54);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(78, 15);
		this.label2.TabIndex = 2;
		this.label2.Text = "Device name:";
		this.txtStationNo.Location = new System.Drawing.Point(111, 119);
		this.txtStationNo.Maximum = new decimal(new int[4] { 10000, 0, 0, 0 });
		this.txtStationNo.Name = "txtStationNo";
		this.txtStationNo.Size = new System.Drawing.Size(66, 23);
		this.txtStationNo.TabIndex = 2;
		this.label3.AutoSize = true;
		this.label3.ForeColor = System.Drawing.SystemColors.ControlText;
		this.label3.Location = new System.Drawing.Point(4, 123);
		this.label3.Name = "label3";
		this.label3.Size = new System.Drawing.Size(66, 15);
		this.label3.TabIndex = 15;
		this.label3.Text = "Station No:";
		base.AutoScaleDimensions = new System.Drawing.SizeF(7f, 15f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(406, 378);
		base.Controls.Add(this.groupBox1);
		base.Controls.Add(this.gbGeneral);
		base.Controls.Add(this.btnCancel);
		base.Controls.Add(this.btnSave);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "MitsubishiSerialDevice";
		this.Text = "Device";
		base.Load += new System.EventHandler(FormDevice_Load);
		((System.ComponentModel.ISupportInitialize)this.errorProvider1).EndInit();
		this.groupBox1.ResumeLayout(false);
		this.groupBox1.PerformLayout();
		this.gbGeneral.ResumeLayout(false);
		this.gbGeneral.PerformLayout();
		((System.ComponentModel.ISupportInitialize)this.txtConnectRetries).EndInit();
		((System.ComponentModel.ISupportInitialize)this.txtStationNo).EndInit();
		base.ResumeLayout(false);
	}
}