﻿/* @Author  Félix-Antoine Guimont
 * @Date    12 octobre 2022
 * @file    Fomr1.cs
 * @brief   
 * 
 */


using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace Labo4_StationMeteo_F.A.Guimont
{
    public partial class Form1 : Form
    {
        Thread objTh;  //On fera tourner l'objet objThUDP dans un Thread pour la rx de la trame
        ThreadRXUDP objUDP;

        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        List<byte> m_lstTrameRx = new List<byte>();
        private int m_alreadyRecieve = 0;
        const Byte SOH = 0x01;
        const int LIMITE_BUFFER = 128;
        const int MAX_TRAME_TOIT = 21;

        enum enumTrame // les différentes positions sont les index des différents bytes
        {
            soh = 0, // début 
            tempEntier,
            tempFraction,
            humidite,
            dirVent,
            vitVent,
            pressionEntier,
            pressionFraction,
            intensiteMsb,
            intensiteLsb,
            tempIntEntier,
            tempIntFraction,
            checksum,
            maxTrame
        };

        enum enumDirVent { Nord = 0, NordEst, Est, SudEst, Sud, SudOuest, Ouest, NordOuest };

        public Form1()
        {
            InitializeComponent();
            
            serialPort.Encoding = Encoding.GetEncoding(28591);    //pour avoir les accents

            objUDP = new ThreadRXUDP(this);
            objUDP.objDelegate = methodeDelegeAffiche; //avoir les threads
            objTh = new Thread(objUDP.FaitTravail);
            objTh.Start();

            affiche_Com();
        }

        /// <summary>
        /// reçoit les bytes du port série
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void port_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            int nbALire;
            byte[] lecture = new byte[LIMITE_BUFFER];
            nbALire = serialPort.BytesToRead;  //on a reçu combien de bytes
            string[] portDispo = System.IO.Ports.SerialPort.GetPortNames();

            if (nbALire > 0)  //Petit test car, de temps en temps on a un événement dataReceive et il n'y a pas de bytes à lire !!!!
            {
                serialPort.Read(lecture, m_alreadyRecieve, nbALire);       // changer le 0 pour quelque chose qui prend le nombre recu et reprendre au nombre qui a lu
                                                                
                for (int i = m_alreadyRecieve; i < m_alreadyRecieve+nbALire; i++)
                {
                    m_lstTrameRx.Add(lecture[i]);
                }

                m_alreadyRecieve += nbALire;

                if(m_alreadyRecieve == (int)enumTrame.maxTrame)
                {
                    if (verifTrame(m_lstTrameRx))
                            BeginInvoke(objUDP.objDelegate, m_lstTrameRx, serialPort.PortName);
                }

                if(m_alreadyRecieve > (int)enumTrame.maxTrame)
                {
                    m_lstTrameRx.Clear();
                    m_alreadyRecieve = 0;
                }
            }
        }

        /// <summary>
        /// met les bytes dans le DataGridView
        /// </summary>
        /// <param name="trame"></param>
        private void methodeDelegeAffiche(List<byte> trame, string srclp)
        {
            if(verifTrame(trame))
            {
                string temps = string.Format("{0:HH:mm:ss tt}", DateTime.Now);  // prend le temps 

                string tempe = Convert.ToString((sbyte)trame[1]);   // convertie en string la température
                string dotTempe = Convert.ToString(trame[2]); // convertie en string la température fractionnaire 
                string humide = Convert.ToString(trame[3]); // convertie en string l'humidité
                string speed = Convert.ToString(trame[5]);  // convertie en string la vitesse du vent
                string windDir = Convert.ToString((enumDirVent)trame[4]);   // convertie en string la direction du vent
                if (windDir.All(char.IsDigit))
                    windDir = "Invalide";

                string press = Convert.ToString(trame[6]);  // convertie en string la pression
                string dotpress = Convert.ToString(trame[7]); // convertie en string la pression fractionnaire

                txtTemperature.Text = tempe + "." + dotTempe;    // écrit dans la case la temépature 
                txtHumidity.Text = humide;  // écrit dans la case l'humidité
                txtWindSpeed.Text = speed;  // écrit dans la case la vitesse du vent
                txtWindDirection.Text = windDir;    // écrit dans la case la direction du vent
                txtPression.Text = press + "." + dotpress;   // écrit dans la case la pression

                dataGridView1.Rows.Insert(0, temps, srclp, tempe + "." + dotTempe, speed, windDir, humide, press + "." + dotpress);   // inseret tout dans la grille 

                dataGridView1.Rows[0].Selected = true;  // sélection la première 
                dataGridView1.Rows[1].Selected = false; // désélection la deuxième

                trame.Clear();   // efface tout dans la trame
                m_lstTrameRx.Clear();
                m_alreadyRecieve = 0;   // efface ou on était rendu 
            }

            else if(verifTrameToit(trame))
            {
                string temps = string.Format("{0:HH:mm:ss tt}", DateTime.Now);  // prend le temps 

                float temp1 = (short)(trame[2] << 8 | trame[3]);
                temp1 = temp1 / 10;
                float temp2 = (byte)(trame[5] << 8 | trame[6]);
                temp2 = temp2 /10;
                float temp3 = (byte)(trame[8] << 8 | trame[9]);
                temp3 = temp3 / 10;
                float temp4 = (byte)(trame[11] << 8 | trame[12]);
                temp4 = temp4 / 10;
                float dirventByte = (byte)(trame[15] << 8 | trame[16]);
                dirventByte = dirventByte / 10;
                float radSunByte = (byte)(trame[18] << 8 | trame[19]);
                radSunByte = radSunByte / 10;
                string temp1Int = Convert.ToString(temp1);
                string temp2Ext = Convert.ToString(temp2);
                string temp3Ext = Convert.ToString(temp3);
                string temp4Int = Convert.ToString(temp4);
                string hum1 = Convert.ToString(trame[4]);
                string hum2 = Convert.ToString(trame[7]);
                string hum3 = Convert.ToString(trame[10]);
                string press = Convert.ToString(trame[13]);
                string pressdot = Convert.ToString(trame[14]);
                string dirVent = Convert.ToString(dirventByte);
                string vitVent = Convert.ToString(trame[17]);
                string radSun = Convert.ToString(radSunByte);

                txtTemperature.Text = temp2Ext;
                txtHumidity.Text = hum2;
                txtWindSpeed.Text = vitVent;
                txtWindDirection.Text = dirVent;
                txtPression.Text = press + "." + pressdot;

                dataGridView1.Rows.Insert(0, temps, srclp, temp1Int, temp2Ext, temp3Ext, temp4Int, hum1, hum2, hum3, press + "." + pressdot, dirVent, vitVent, radSun);

                dataGridView1.Rows[0].Selected = true;  // sélection la première 
                dataGridView1.Rows[1].Selected = false; // désélection la deuxième

                trame.Clear();   // efface tout dans la trame
                m_lstTrameRx.Clear();
                m_alreadyRecieve = 0;   // efface ou on était rendu 
            }
        }

        /// <summary>
        /// affiche le statue du port série
        /// </summary>
        private void affiche_Com()
        {
            if (serialPort.IsOpen) // si le port est ouvert
            {
                toolStripStatusLabel1.Text = serialPort.PortName + ':' + serialPort.BaudRate + ',' + serialPort.Parity + ',' + serialPort.DataBits + ',' + serialPort.StopBits;   //affiche les informations importantes du port 
                toolStripStatusLabel2.Text = "Open";    // affiche qu'il est ouvert
                toolStripStatusLabel2.ForeColor = Color.Green;  // met le mot Open en Vert
            }

            else    // si le port n'est pas ouvert
            {
                toolStripStatusLabel1.Text = serialPort.PortName + ':' + serialPort.BaudRate + ',' + serialPort.Parity + ',' + serialPort.DataBits + ',' + serialPort.StopBits;
                toolStripStatusLabel2.Text = "Close";   // affiche qu'il est fermer
                toolStripStatusLabel2.ForeColor = Color.Red;    // met le mot close en rouge
            }

        }

        /// <summary>
        /// ouvre la page de configuration de port série
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void portToolStripMenuItem_Click(object sender, EventArgs e)
        {
            serialPort.Close();   // ferme le port
            affiche_Com();  // rafraichit l'affichage de l'état du port

            frmConfigPort fConfig = new frmConfigPort(serialPort.PortName, serialPort.BaudRate, serialPort.DataBits, serialPort.Parity, (int)serialPort.StopBits);    // crée une variable instancie vers frmConfigPort

            if (fConfig.ShowDialog() == DialogResult.OK) //Note: La propriété DialogResult du bouton ok doit être à OK.
            {
                //On récupère les informations de configurations et on les assignent à au port série
                if (!(fConfig.m_nom == null))       // s'il y a un port choisi
                    serialPort.PortName = fConfig.m_nom;  // met le nom du port dans port.PortName

                else    //s'il n'y a pas de port choisi
                    MessageBox.Show("aucun com à été choisi");

                serialPort.BaudRate = fConfig.m_vitesse;  // met la vitesse dans le baudrate 
                serialPort.DataBits = fConfig.m_nbBit;    // met le nombre de bits dans le dataBits 
                serialPort.Parity = fConfig.m_parite; // met la parité dans le parity 
                serialPort.StopBits = (System.IO.Ports.StopBits)fConfig.m_stopBit;   // met le stop bits dans une variable 

                try
                {
                    serialPort.Open();    // ouvre le port
                    affiche_Com();
                }

                catch
                {
                    MessageBox.Show("Ne peux pas ouvrir " + serialPort.PortName);  // affiche un message si nous pouvons pas ouvrir sur le port
                }
            }

            else
            {
                try
                {
                    serialPort.Open();    // ouvre le port
                    affiche_Com();
                }

                catch
                {
                    MessageBox.Show("Ne peux pas ouvrir " + serialPort.PortName);  // affiche un message si nous pouvons pas ouvrir sur le port
                }
            }
        }

        /// <summary>
        /// quitte l'application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void quitterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit(); // ferme l'application
        }

        /// <summary>
        /// ouvre la page d'explorateur de fichier windows et l'enregistre en csv
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void enregistrerSousToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string temps = string.Format("{0:HH:mm:ss tt}", DateTime.Now);  // prend le temps 
            string date = DateTime.Now.ToString("MMMM/dddd dd/yyyy");   // prend la date 
            StreamWriter swFichier; // crée une variable pour écrire dans un fichier
            int i, j;   

            this.saveFileDialog1 = new SaveFileDialog(); // va sauvegarder le fichier
            saveFileDialog1.Filter = "Text files (*.csv)|*.csv|All files (*.*)|*.*";    // le sauvegarde en csv

            if(saveFileDialog1.ShowDialog() == DialogResult.OK) // si dans l'explorateur de fichier il dit OK
            {
                swFichier = File.CreateText(saveFileDialog1.FileName);  // crée un fichier avec le nom qu'il lui a donner
                swFichier.Write("Donnée de la station météo " + date + " " + temps);    // met la date et le temps
                swFichier.Write("\n\r");    // met un espace de ligne 
                for (j=0; j< dataGridView1.RowCount-1; j++) // prend la ligne d'information
                {
                    for (i = 0; i < dataGridView1.ColumnCount; i++) // prend les informations de la colonne 
                    {
                        swFichier.Write((string)(dataGridView1.Rows[j].Cells[i].Value)+ " ;");  // l'écrit dans le fichier
                    }
                    swFichier.Write("\n\r");
                }
                swFichier.Close();  // ferme le fichier
            }
        }

        /// <summary>
        /// ouvre la page d'explorateur de fichier windows
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            enregistrerSousToolStripMenuItem.PerformClick();    // fait comme il click sur le menu d'enregistrer
        }

        /// <summary>
        /// ouvre la page de configuration de port série
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            portToolStripMenuItem.PerformClick();   // fait comme il click sur le menu de la configuration du port série
        }

        /// <summary>
        /// Retourne vrai, si la trame est valide. Faux si la trame est incomplète ou invalide.
        /// </summary>
        /// <param name="trame"></param>
        /// <returns></returns>
        private bool verifTrame(List<byte> trame) 
        {
            int ck = 0;

            if (trame[0] != SOH)    //si le SOH n'est pas présent
            {
                trame.Clear();
                m_alreadyRecieve = 0;
                return false;
            }

            if (trame.Count > (int)enumTrame.maxTrame) // si le compte de bytes n'est pas bon 
                return false;

            ck = calculChecksum(trame);

            if (trame[12] < ck || trame[12] > ck)  // si le checksum ne correspond pas a celui envoyer
            {
                trame.Clear();
                m_alreadyRecieve = 0;
                return false;
            }

            return true;    // retourne vrai
        }

        /// <summary>
        /// calcul le checksum (addition des octets de la trame et retourne la valeur des 8 LSB.)
        /// </summary>
        /// <param name="trame"></param>
        /// <returns></returns>
        private byte calculChecksum(List<byte> trame) 
        {
            int ck = 0;

            for (int i = 1; i < trame.Count - 1; i++)   //fait le calcul pour le CheckSum
            {
                ck += trame[i];
            }

            ck = ck & 0x00FF;   // prend les 8 LSB du CheckSum

            return (byte)ck;    //retourne em bytes le CheckSum
        }

        private bool verifTrameToit(List<byte> trame)
        {
            int ck = 0;

            if (trame[0] != SOH)    //si le SOH n'est pas présent
            {
                trame.Clear();
                m_alreadyRecieve = 0;
                return false;
            }

            if (trame.Count > MAX_TRAME_TOIT) // si le compte de bytes n'est pas bon 
                return false;

            ck = calculChecksum(trame) -1 ;

            if (trame[20] < ck || trame[20] > ck)  // si le checksum ne correspond pas a celui envoyer
            {
                trame.Clear();
                m_alreadyRecieve = 0;
                return false;
            }

            return true;
        }

        /// <summary>
        /// s'applique quand on ferme la page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            objUDP.ArreteClientUDP();
            objTh.Abort();
        }
    }
}
