using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Windows.Forms;
using NetStudio.Common;
using NetStudio.Common.DataTypes;
using NetStudio.Common.IndusCom;
using NetStudio.Common.Manager;
using NetStudio.Delta.Models;
using NetStudio.IPS.Local;

namespace NetStudio.IPS;

public class DeltaTcpDevice : Form
{
	private Channel channel;

	private Device? device;

	public EventDeviceChanged? OnDeviceChanged;

	private IContainer components;

	private Label label1;

	private TextBox txtChannel;

	private Label label2;

	private TextBox txtDeviceName;

	private Button btnCancel;

	private Button btnSave;

	private ErrorProvider errorProvider1;

	private NumericUpDown txtStationNo;

	private Label label3;

	private CheckBox chkActive;

	private TextBox txtDescription;

	private CheckBox chkAutoReconnect;

	private GroupBox groupBox1;

	private GroupBox gbGeneral;

	private GroupBox groupBox2;

	private Label label8;

	private NumericUpDown txtReceivingDelay;

	private NumericUpDown txtReceiveTimeout;

	private Label label10;

	private NumericUpDown txtSendTimeout;

	private Label label9;

	private GroupBox gbEthernet;

	private NumericUpDown txtPort;

	private Label lblPort;

	private TextBox txtIPAddress;

	private Label lblIPAddress;

	private ComboBox cboxDeviceType;

	private Label label4;

	private NumericUpDown txtConnectRetries;

	private Label label6;

	public DeltaTcpDevice(Channel channel, Device? device = null)
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
				this.device = new Device
				{
					Adapter = new EthernetAdapter()
				};
				this.device.BlockSize = this.device.GetBlockSize(this.channel.Protocol);
			}
			this.device.BlockSize = 100;
			this.device.ByteOrder = ByteOrder.BigEndian;
			if (channel.ConnectionType == ConnectionType.Serial)
			{
				this.device.Adapter = null;
			}
			else
			{
				this.device.Adapter = this.device.Adapter ?? new EthernetAdapter(ProtocolType.Tcp);
			}
			this.device.ChannelId = channel.Id;
			txtDeviceName.DataBindings.Add("Text", this.device, "Name", formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged);
			txtStationNo.DataBindings.Add("Value", this.device, "StationNo", formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged);
			chkActive.DataBindings.Add("Checked", this.device, "Active", formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged);
			chkAutoReconnect.DataBindings.Add("Checked", this.device, "AutoReconnect", formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged);
			txtConnectRetries.DataBindings.Add("Value", this.device, "ConnectRetries", formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged);
			txtDescription.DataBindings.Add("Text", this.device, "Description", formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged);
			txtIPAddress.DataBindings.Add("Text", this.device.Adapter, "IPAddress", formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged);
			txtPort.DataBindings.Add("Value", this.device.Adapter, "Port", formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged);
			txtSendTimeout.DataBindings.Add("Value", this.device.Adapter, "SendTimeout", formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged);
			txtReceiveTimeout.DataBindings.Add("Value", this.device.Adapter, "ReceiveTimeout", formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged);
			txtReceivingDelay.DataBindings.Add("Value", this.device, "ReceivingDelay", formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged);
			cboxDeviceType.DisplayMember = "Value";
			cboxDeviceType.ValueMember = "Key";
			cboxDeviceType.DataSource = Extensions.GetDictionary<CPUType>().ToList();
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

	private void FormVigorDevice_Load(object sender, EventArgs e)
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
			if (!ValidateInputs())
			{
				return;
			}
			if (device == null)
			{
				throw new Exception("Device is null");
			}
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

	private bool ValidateInputs()
	{
		errorProvider1.Clear();
		if (string.IsNullOrEmpty(txtIPAddress.Text) || string.IsNullOrWhiteSpace(txtIPAddress.Text))
		{
			errorProvider1.SetIconAlignment(txtIPAddress, ErrorIconAlignment.MiddleLeft);
			errorProvider1.SetIconPadding(txtIPAddress, 4);
			errorProvider1.SetError(txtIPAddress, "Please enter your IP Address");
		}
		if (!Utility.IsIpV4AddressValid(txtIPAddress.Text))
		{
			errorProvider1.SetIconAlignment(txtIPAddress, ErrorIconAlignment.MiddleLeft);
			errorProvider1.SetIconPadding(txtIPAddress, 4);
			errorProvider1.SetError(txtIPAddress, "IP Address: Invalid.");
		}
		if (string.IsNullOrEmpty(txtDeviceName.Text) || string.IsNullOrWhiteSpace(txtDeviceName.Text))
		{
			errorProvider1.SetIconAlignment(txtDeviceName, ErrorIconAlignment.MiddleLeft);
			errorProvider1.SetIconPadding(txtDeviceName, 4);
			errorProvider1.SetError(txtDeviceName, "Please enter your device name");
		}
		return !errorProvider1.HasErrors;
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NetStudio.IPS.DeltaTcpDevice));
		this.label1 = new System.Windows.Forms.Label();
		this.txtChannel = new System.Windows.Forms.TextBox();
		this.label2 = new System.Windows.Forms.Label();
		this.txtDeviceName = new System.Windows.Forms.TextBox();
		this.btnCancel = new System.Windows.Forms.Button();
		this.btnSave = new System.Windows.Forms.Button();
		this.errorProvider1 = new System.Windows.Forms.ErrorProvider(this.components);
		this.label3 = new System.Windows.Forms.Label();
		this.txtStationNo = new System.Windows.Forms.NumericUpDown();
		this.txtDescription = new System.Windows.Forms.TextBox();
		this.chkActive = new System.Windows.Forms.CheckBox();
		this.chkAutoReconnect = new System.Windows.Forms.CheckBox();
		this.gbGeneral = new System.Windows.Forms.GroupBox();
		this.txtConnectRetries = new System.Windows.Forms.NumericUpDown();
		this.label6 = new System.Windows.Forms.Label();
		this.cboxDeviceType = new System.Windows.Forms.ComboBox();
		this.label4 = new System.Windows.Forms.Label();
		this.groupBox1 = new System.Windows.Forms.GroupBox();
		this.groupBox2 = new System.Windows.Forms.GroupBox();
		this.label8 = new System.Windows.Forms.Label();
		this.txtReceivingDelay = new System.Windows.Forms.NumericUpDown();
		this.txtReceiveTimeout = new System.Windows.Forms.NumericUpDown();
		this.label10 = new System.Windows.Forms.Label();
		this.txtSendTimeout = new System.Windows.Forms.NumericUpDown();
		this.label9 = new System.Windows.Forms.Label();
		this.gbEthernet = new System.Windows.Forms.GroupBox();
		this.txtPort = new System.Windows.Forms.NumericUpDown();
		this.lblPort = new System.Windows.Forms.Label();
		this.txtIPAddress = new System.Windows.Forms.TextBox();
		this.lblIPAddress = new System.Windows.Forms.Label();
		((System.ComponentModel.ISupportInitialize)this.errorProvider1).BeginInit();
		((System.ComponentModel.ISupportInitialize)this.txtStationNo).BeginInit();
		this.gbGeneral.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.txtConnectRetries).BeginInit();
		this.groupBox1.SuspendLayout();
		this.groupBox2.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.txtReceivingDelay).BeginInit();
		((System.ComponentModel.ISupportInitialize)this.txtReceiveTimeout).BeginInit();
		((System.ComponentModel.ISupportInitialize)this.txtSendTimeout).BeginInit();
		this.gbEthernet.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.txtPort).BeginInit();
		base.SuspendLayout();
		this.label1.AutoSize = true;
		this.label1.ForeColor = System.Drawing.SystemColors.ControlText;
		this.label1.Location = new System.Drawing.Point(4, 21);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(87, 15);
		this.label1.TabIndex = 0;
		this.label1.Text = "Channel name:";
		this.txtChannel.Location = new System.Drawing.Point(106, 18);
		this.txtChannel.Name = "txtChannel";
		this.txtChannel.ReadOnly = true;
		this.txtChannel.Size = new System.Drawing.Size(302, 23);
		this.txtChannel.TabIndex = 9;
		this.label2.AutoSize = true;
		this.label2.ForeColor = System.Drawing.SystemColors.ControlText;
		this.label2.Location = new System.Drawing.Point(4, 54);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(78, 15);
		this.label2.TabIndex = 2;
		this.label2.Text = "Device name:";
		this.txtDeviceName.Location = new System.Drawing.Point(106, 50);
		this.txtDeviceName.Name = "txtDeviceName";
		this.txtDeviceName.Size = new System.Drawing.Size(302, 23);
		this.txtDeviceName.TabIndex = 1;
		this.txtDeviceName.Validating += new System.ComponentModel.CancelEventHandler(txtDeviceName_Validating);
		this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		this.btnCancel.Location = new System.Drawing.Point(351, 431);
		this.btnCancel.Name = "btnCancel";
		this.btnCancel.Size = new System.Drawing.Size(76, 30);
		this.btnCancel.TabIndex = 8;
		this.btnCancel.Text = "Cancel";
		this.btnCancel.UseVisualStyleBackColor = true;
		this.btnSave.Location = new System.Drawing.Point(269, 431);
		this.btnSave.Name = "btnSave";
		this.btnSave.Size = new System.Drawing.Size(76, 30);
		this.btnSave.TabIndex = 7;
		this.btnSave.Text = "Save";
		this.btnSave.UseVisualStyleBackColor = true;
		this.btnSave.Click += new System.EventHandler(btnSave_Click);
		this.errorProvider1.ContainerControl = this;
		this.label3.AutoSize = true;
		this.label3.ForeColor = System.Drawing.SystemColors.ControlText;
		this.label3.Location = new System.Drawing.Point(4, 87);
		this.label3.Name = "label3";
		this.label3.Size = new System.Drawing.Size(66, 15);
		this.label3.TabIndex = 15;
		this.label3.Text = "Station No:";
		this.txtStationNo.Location = new System.Drawing.Point(106, 83);
		this.txtStationNo.Maximum = new decimal(new int[4] { 10000, 0, 0, 0 });
		this.txtStationNo.Name = "txtStationNo";
		this.txtStationNo.Size = new System.Drawing.Size(79, 23);
		this.txtStationNo.TabIndex = 2;
		this.txtDescription.Location = new System.Drawing.Point(10, 24);
		this.txtDescription.Multiline = true;
		this.txtDescription.Name = "txtDescription";
		this.txtDescription.Size = new System.Drawing.Size(398, 100);
		this.txtDescription.TabIndex = 6;
		this.chkActive.AutoSize = true;
		this.chkActive.ForeColor = System.Drawing.SystemColors.ControlText;
		this.chkActive.Location = new System.Drawing.Point(8, 119);
		this.chkActive.Name = "chkActive";
		this.chkActive.Size = new System.Drawing.Size(59, 19);
		this.chkActive.TabIndex = 3;
		this.chkActive.Text = "Active";
		this.chkActive.UseVisualStyleBackColor = true;
		this.chkAutoReconnect.AutoSize = true;
		this.chkAutoReconnect.ForeColor = System.Drawing.SystemColors.ControlText;
		this.chkAutoReconnect.Location = new System.Drawing.Point(106, 119);
		this.chkAutoReconnect.Name = "chkAutoReconnect";
		this.chkAutoReconnect.Size = new System.Drawing.Size(108, 19);
		this.chkAutoReconnect.TabIndex = 41;
		this.chkAutoReconnect.Text = "Auto reconnect";
		this.chkAutoReconnect.UseVisualStyleBackColor = true;
		this.gbGeneral.Controls.Add(this.txtConnectRetries);
		this.gbGeneral.Controls.Add(this.label6);
		this.gbGeneral.Controls.Add(this.cboxDeviceType);
		this.gbGeneral.Controls.Add(this.label4);
		this.gbGeneral.Controls.Add(this.txtDeviceName);
		this.gbGeneral.Controls.Add(this.label1);
		this.gbGeneral.Controls.Add(this.chkAutoReconnect);
		this.gbGeneral.Controls.Add(this.txtChannel);
		this.gbGeneral.Controls.Add(this.chkActive);
		this.gbGeneral.Controls.Add(this.label2);
		this.gbGeneral.Controls.Add(this.txtStationNo);
		this.gbGeneral.Controls.Add(this.label3);
		this.gbGeneral.ForeColor = System.Drawing.Color.Navy;
		this.gbGeneral.Location = new System.Drawing.Point(9, 3);
		this.gbGeneral.Name = "gbGeneral";
		this.gbGeneral.Size = new System.Drawing.Size(418, 152);
		this.gbGeneral.TabIndex = 42;
		this.gbGeneral.TabStop = false;
		this.gbGeneral.Text = "General";
		this.txtConnectRetries.Location = new System.Drawing.Point(347, 117);
		this.txtConnectRetries.Maximum = new decimal(new int[4] { 3600, 0, 0, 0 });
		this.txtConnectRetries.Minimum = new decimal(new int[4] { 1, 0, 0, 0 });
		this.txtConnectRetries.Name = "txtConnectRetries";
		this.txtConnectRetries.Size = new System.Drawing.Size(61, 23);
		this.txtConnectRetries.TabIndex = 47;
		this.txtConnectRetries.Value = new decimal(new int[4] { 3, 0, 0, 0 });
		this.label6.AutoSize = true;
		this.label6.ForeColor = System.Drawing.SystemColors.ControlText;
		this.label6.Location = new System.Drawing.Point(248, 121);
		this.label6.Name = "label6";
		this.label6.Size = new System.Drawing.Size(90, 15);
		this.label6.TabIndex = 48;
		this.label6.Text = "Connect retries:";
		this.cboxDeviceType.FormattingEnabled = true;
		this.cboxDeviceType.Location = new System.Drawing.Point(287, 82);
		this.cboxDeviceType.Name = "cboxDeviceType";
		this.cboxDeviceType.Size = new System.Drawing.Size(121, 23);
		this.cboxDeviceType.TabIndex = 46;
		this.label4.AutoSize = true;
		this.label4.ForeColor = System.Drawing.SystemColors.ControlText;
		this.label4.Location = new System.Drawing.Point(248, 87);
		this.label4.Name = "label4";
		this.label4.Size = new System.Drawing.Size(33, 15);
		this.label4.TabIndex = 45;
		this.label4.Text = "CPU:";
		this.groupBox1.Controls.Add(this.txtDescription);
		this.groupBox1.ForeColor = System.Drawing.Color.Navy;
		this.groupBox1.Location = new System.Drawing.Point(9, 158);
		this.groupBox1.Name = "groupBox1";
		this.groupBox1.Size = new System.Drawing.Size(418, 138);
		this.groupBox1.TabIndex = 43;
		this.groupBox1.TabStop = false;
		this.groupBox1.Text = "Description";
		this.groupBox2.Controls.Add(this.label8);
		this.groupBox2.Controls.Add(this.txtReceivingDelay);
		this.groupBox2.Controls.Add(this.txtReceiveTimeout);
		this.groupBox2.Controls.Add(this.label10);
		this.groupBox2.Controls.Add(this.txtSendTimeout);
		this.groupBox2.Controls.Add(this.label9);
		this.groupBox2.ForeColor = System.Drawing.Color.Navy;
		this.groupBox2.Location = new System.Drawing.Point(239, 302);
		this.groupBox2.Name = "groupBox2";
		this.groupBox2.Size = new System.Drawing.Size(188, 123);
		this.groupBox2.TabIndex = 85;
		this.groupBox2.TabStop = false;
		this.groupBox2.Text = "Set time: miniseconds";
		this.label8.AutoSize = true;
		this.label8.ForeColor = System.Drawing.SystemColors.ControlText;
		this.label8.Location = new System.Drawing.Point(16, 90);
		this.label8.Name = "label8";
		this.label8.Size = new System.Drawing.Size(81, 15);
		this.label8.TabIndex = 24;
		this.label8.Text = "Receive delay:";
		this.txtReceivingDelay.Location = new System.Drawing.Point(110, 86);
		this.txtReceivingDelay.Maximum = new decimal(new int[4] { 150, 0, 0, 0 });
		this.txtReceivingDelay.Name = "txtReceivingDelay";
		this.txtReceivingDelay.Size = new System.Drawing.Size(68, 23);
		this.txtReceivingDelay.TabIndex = 15;
		this.txtReceiveTimeout.Location = new System.Drawing.Point(110, 55);
		this.txtReceiveTimeout.Maximum = new decimal(new int[4] { 10000, 0, 0, 0 });
		this.txtReceiveTimeout.Minimum = new decimal(new int[4] { 250, 0, 0, 0 });
		this.txtReceiveTimeout.Name = "txtReceiveTimeout";
		this.txtReceiveTimeout.Size = new System.Drawing.Size(68, 23);
		this.txtReceiveTimeout.TabIndex = 14;
		this.txtReceiveTimeout.Value = new decimal(new int[4] { 250, 0, 0, 0 });
		this.label10.AutoSize = true;
		this.label10.ForeColor = System.Drawing.SystemColors.ControlText;
		this.label10.Location = new System.Drawing.Point(3, 59);
		this.label10.Name = "label10";
		this.label10.Size = new System.Drawing.Size(95, 15);
		this.label10.TabIndex = 22;
		this.label10.Text = "Receive timeout:";
		this.txtSendTimeout.Location = new System.Drawing.Point(110, 24);
		this.txtSendTimeout.Maximum = new decimal(new int[4] { 10000, 0, 0, 0 });
		this.txtSendTimeout.Minimum = new decimal(new int[4] { 250, 0, 0, 0 });
		this.txtSendTimeout.Name = "txtSendTimeout";
		this.txtSendTimeout.Size = new System.Drawing.Size(68, 23);
		this.txtSendTimeout.TabIndex = 13;
		this.txtSendTimeout.Value = new decimal(new int[4] { 250, 0, 0, 0 });
		this.label9.AutoSize = true;
		this.label9.ForeColor = System.Drawing.SystemColors.ControlText;
		this.label9.Location = new System.Drawing.Point(17, 28);
		this.label9.Name = "label9";
		this.label9.Size = new System.Drawing.Size(81, 15);
		this.label9.TabIndex = 20;
		this.label9.Text = "Send timeout:";
		this.gbEthernet.Controls.Add(this.txtPort);
		this.gbEthernet.Controls.Add(this.lblPort);
		this.gbEthernet.Controls.Add(this.txtIPAddress);
		this.gbEthernet.Controls.Add(this.lblIPAddress);
		this.gbEthernet.ForeColor = System.Drawing.Color.Navy;
		this.gbEthernet.Location = new System.Drawing.Point(9, 302);
		this.gbEthernet.Name = "gbEthernet";
		this.gbEthernet.Size = new System.Drawing.Size(224, 123);
		this.gbEthernet.TabIndex = 84;
		this.gbEthernet.TabStop = false;
		this.gbEthernet.Text = "Ethernet";
		this.txtPort.Location = new System.Drawing.Point(86, 53);
		this.txtPort.Maximum = new decimal(new int[4] { 100000, 0, 0, 0 });
		this.txtPort.Name = "txtPort";
		this.txtPort.Size = new System.Drawing.Size(125, 23);
		this.txtPort.TabIndex = 6;
		this.txtPort.Value = new decimal(new int[4] { 502, 0, 0, 0 });
		this.lblPort.AutoSize = true;
		this.lblPort.ForeColor = System.Drawing.SystemColors.ControlText;
		this.lblPort.Location = new System.Drawing.Point(38, 57);
		this.lblPort.Name = "lblPort";
		this.lblPort.Size = new System.Drawing.Size(32, 15);
		this.lblPort.TabIndex = 16;
		this.lblPort.Text = "Port:";
		this.txtIPAddress.Location = new System.Drawing.Point(86, 20);
		this.txtIPAddress.Name = "txtIPAddress";
		this.txtIPAddress.Size = new System.Drawing.Size(125, 23);
		this.txtIPAddress.TabIndex = 5;
		this.txtIPAddress.Text = "127.0.0.1";
		this.lblIPAddress.AutoSize = true;
		this.lblIPAddress.ForeColor = System.Drawing.SystemColors.ControlText;
		this.lblIPAddress.Location = new System.Drawing.Point(5, 24);
		this.lblIPAddress.Name = "lblIPAddress";
		this.lblIPAddress.Size = new System.Drawing.Size(65, 15);
		this.lblIPAddress.TabIndex = 14;
		this.lblIPAddress.Text = "IP Address:";
		base.AutoScaleDimensions = new System.Drawing.SizeF(7f, 15f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(438, 472);
		base.Controls.Add(this.groupBox2);
		base.Controls.Add(this.gbEthernet);
		base.Controls.Add(this.groupBox1);
		base.Controls.Add(this.gbGeneral);
		base.Controls.Add(this.btnCancel);
		base.Controls.Add(this.btnSave);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "DeltaTcpDevice";
		this.Text = "Device";
		base.Load += new System.EventHandler(FormVigorDevice_Load);
		((System.ComponentModel.ISupportInitialize)this.errorProvider1).EndInit();
		((System.ComponentModel.ISupportInitialize)this.txtStationNo).EndInit();
		this.gbGeneral.ResumeLayout(false);
		this.gbGeneral.PerformLayout();
		((System.ComponentModel.ISupportInitialize)this.txtConnectRetries).EndInit();
		this.groupBox1.ResumeLayout(false);
		this.groupBox1.PerformLayout();
		this.groupBox2.ResumeLayout(false);
		this.groupBox2.PerformLayout();
		((System.ComponentModel.ISupportInitialize)this.txtReceivingDelay).EndInit();
		((System.ComponentModel.ISupportInitialize)this.txtReceiveTimeout).EndInit();
		((System.ComponentModel.ISupportInitialize)this.txtSendTimeout).EndInit();
		this.gbEthernet.ResumeLayout(false);
		this.gbEthernet.PerformLayout();
		((System.ComponentModel.ISupportInitialize)this.txtPort).EndInit();
		base.ResumeLayout(false);
	}
}
