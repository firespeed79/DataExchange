using DataExchangeCommon;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MapSetting
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadMaps();
        }

        private void LoadMaps()
        {
            listBox1.Items.Clear();
            listBox2.Items.Clear();
            try
            {
                MapManager.LoadMap();
                AddressMap map = MapManager.Map;
                foreach (AddressAndPort from in map.Froms)
                {
                    listBox1.Items.Add(from.ToString());
                    List<AddressAndPort> tos = map[from].Item2;
                    StringBuilder sb = new StringBuilder();
                    tos.ForEach(e => sb.Append(e.ToString() + "|"));
                    string sbb = sb.ToString();
                    if (sb.Length > 0)
                        sbb = sbb.Substring(0, sbb.Length - 1);
                    listBox2.Items.Add(sbb);
                }
                listBox1.Tag = map;
            }
            catch (Exception ex)
            {
                MessageBox.Show("载入映射失败：" + ex.Message);
            }
            finally
            {
                MapManager.Dispose();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            AddressAndPort from = new AddressAndPort() { Address = textBox1.Text.Trim(), Port = (int)numericUpDown1.Value };
            if (!listBox1.Items.Contains(from))
            {
                AddressAndPort to = new AddressAndPort() { Address = textBox2.Text.Trim(), Port = (int)numericUpDown2.Value };
                List<AddressAndPort> tos = new List<AddressAndPort>();
                tos.Add(to);
                int id = MapManager.AddMap(new KeyValuePair<AddressAndPort, List<AddressAndPort>>(from, tos), textBox3.Text.Trim());
                if (id > 0)
                {
                    AddressMap map = listBox1.Tag as AddressMap;
                    map.Add(id, from, to, textBox3.Text.Trim());
                    listBox1.Items.Add(from);
                    listBox2.Items.Add(to);
                    if (MessageBox.Show("添加映射成功！是否为双向映射？确定将自动为您添加一个反向映射。", "添加成功和双向提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                    {
                        AddressAndPort from2 = new AddressAndPort() { Address = textBox2.Text.Trim(), Port = (int)numericUpDown2.Value };
                        if (!listBox1.Items.Contains(from2))
                        {
                            AddressAndPort to2 = new AddressAndPort() { Address = textBox1.Text.Trim(), Port = (int)numericUpDown1.Value };
                            List<AddressAndPort> tos2 = new List<AddressAndPort>();
                            tos2.Add(to2);
                            id = MapManager.AddMap(new KeyValuePair<AddressAndPort, List<AddressAndPort>>(from2, tos2), textBox3.Text.Trim() + "_反向");
                            if (id > 0)
                            {
                                map.Add(id, from2, to2, textBox3.Text.Trim() + "_反向");
                                listBox1.Items.Add(from2);
                                listBox2.Items.Add(to2);
                                MessageBox.Show("添加反向映射成功！");
                            }
                        }
                        else
                        {
                            MessageBox.Show("已存在该映射起点[" + from2 + "]，请点击修改按钮！");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("添加映射失败！");
                }
                MapManager.Dispose();
            }
            else
            {
                MessageBox.Show("已存在该映射起点[" + from + "]，请点击修改按钮！");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
            {
                AddressMap map = listBox1.Tag as AddressMap;
                AddressAndPort f = MapManager.ParseFrom(listBox1.Items[listBox1.SelectedIndex].ToString());
                int id = map[f].Item1;
                AddressAndPort from = new AddressAndPort() { Address = textBox1.Text.Trim(), Port = (int)numericUpDown1.Value };
                List<AddressAndPort> to = MapManager.ParseTo(listBox2.Items[listBox1.SelectedIndex].ToString());
                if (MapManager.UpdateMap(id, new KeyValuePair<AddressAndPort, List<AddressAndPort>>(from, to), textBox3.Text.Trim()))
                {
                    map[f] = new Tuple<int, List<AddressAndPort>, string>(id, to, textBox3.Text.Trim());
                    listBox1.Items[listBox1.SelectedIndex] = from;
                    MessageBox.Show("修改映射成功！");
                }
                else
                {
                    MessageBox.Show("修改映射失败！");
                }
                MapManager.Dispose();
            }
            else
            {
                MessageBox.Show("请选定一个映射！");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
            {
                if (MessageBox.Show("确定要删除该映射么？", "删除提醒", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    Tuple<int, List<AddressAndPort>> tos = listBox1.Tag as Tuple<int, List<AddressAndPort>>;
                    int id = tos.Item1;
                    AddressAndPort from = new AddressAndPort() { Address = textBox1.Text.Trim(), Port = (int)numericUpDown1.Value };
                    if (MapManager.DeleteMap(id))
                    {
                        AddressMap map = listBox1.Tag as AddressMap;
                        map.RemoveFrom(from);
                        listBox1.Items.RemoveAt(listBox1.SelectedIndex);
                        listBox2.Items.RemoveAt(listBox1.SelectedIndex);
                        MessageBox.Show("删除成功！");
                    }
                    else
                    {
                        MessageBox.Show("删除失败！");
                    }
                    MapManager.Dispose();
                }
            }
            else
            {
                MessageBox.Show("请选定一个映射！");
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBox2.SelectedIndex = listBox1.SelectedIndex;
            AddressMap map = listBox1.Tag as AddressMap;
            AddressAndPort from = MapManager.ParseFrom(listBox1.Items[listBox1.SelectedIndex].ToString());
            textBox1.Text = from.Address;
            numericUpDown1.Value = from.Port;
            textBox3.Text = map.GetRemark(from);
            List<AddressAndPort> tos = MapManager.ParseTo(listBox2.Items[listBox1.SelectedIndex].ToString());
            if (tos.Count > 0)
            {
                AddressAndPort to = tos[0];
                textBox2.Text = to.Address;
                numericUpDown2.Value = to.Port;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (listBox2.SelectedIndex > -1)
            {
                string orgine = listBox2.Items[listBox2.SelectedIndex].ToString();
                List<AddressAndPort> tos = MapManager.ParseTo(orgine);
                AddressAndPort to = new AddressAndPort() { Address = textBox2.Text.Trim(), Port = (int)numericUpDown2.Value };
                if (tos.Contains(to))
                {
                    MessageBox.Show("映射终点中已经存在！追加失败。");
                }
                else
                {
                    tos.Add(to);
                    StringBuilder sb = new StringBuilder();
                    foreach (AddressAndPort ap in tos)
                    {
                        if (sb.Length == 0)
                            sb.Append(ap.ToString());
                        else
                            sb.Append("|" + ap.ToString());
                    }
                    listBox2.Items[listBox2.SelectedIndex] = sb.ToString();
                    MessageBox.Show("追加成功！但请注意：需要点击上方的修改按钮才会保存到数据库");
                }
            }
            else
            {
                MessageBox.Show("请选定一个映射终点！");
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (listBox2.SelectedIndex > -1)
            {
                if (MessageBox.Show("确定要删除该映射么？", "删除提醒", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    string orgine = listBox2.Items[listBox2.SelectedIndex].ToString();
                    List<AddressAndPort> tos = MapManager.ParseTo(orgine);
                    AddressAndPort to = new AddressAndPort() { Address = textBox2.Text.Trim(), Port = (int)numericUpDown2.Value };
                    if (tos.Contains(to))
                    {
                        tos.Remove(to);
                        StringBuilder sb = new StringBuilder();
                        foreach (AddressAndPort ap in tos)
                        {
                            if (sb.Length == 0)
                                sb.Append(ap.ToString());
                            else
                                sb.Append("|" + ap.ToString());
                        }
                        listBox2.Items[listBox2.SelectedIndex] = sb.ToString();
                        MessageBox.Show("删除成功！但请注意：需要点击上方的修改按钮才会保存到数据库");
                    }
                    else
                    {
                        MessageBox.Show("映射终点中不存在！删除失败。");
                    }
                }
            }
            else
            {
                MessageBox.Show("请选定一个映射终点！");
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (listBox2.SelectedIndex > -1)
            {
                if (MessageBox.Show("确定要清空该映射么？", "清空提醒", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    listBox2.Items[listBox2.SelectedIndex] = "";
                    MessageBox.Show("清空成功！但请注意：需要点击上方的修改按钮才会保存到数据库");
                }
            }
            else
            {
                MessageBox.Show("请选定一个映射终点！");
            }
        }
    }
}
