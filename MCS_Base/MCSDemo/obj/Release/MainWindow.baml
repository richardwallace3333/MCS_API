   M S B A M L   `   `   `  ���� F  BPalletCheck, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null)clr-namespace:PalletCheckPalletCheck  ;"clr-namespace:PalletCheck.ControlsPalletCheck.Controls      PalletCheck.MainWindow   U QScottPlot.WPF, Version=4.1.27.0, Culture=neutral, PublicKeyToken=e53b06131e34a3aaR NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     +localclr-namespace:PalletCheck     7controls"clr-namespace:PalletCheck.Controls     -     � Title$  PalletCheck v1.0��$
��1050��$
��1920�� � WindowStartupLocation$ CenterScreen=� � WindowState$ 	Maximized=� � 
ResizeMode$ 	CanResize=�����  �� EndPoint$ 0.5,1&� �� 
StartPoint$ 0.5,0&�.���  � Color$ Black�� � Offset$ 0Q�� $ 	#FF003E5B��$ 1Q�.���  � ColumnDefinitions �� $��7 ��� $��11 ��� $��85 ��� $��1187* ��� $
��630* �	 � RowDefinitions	 �� $��50 ��� $��* �.+�]� $��2��$��3��$
��Left=�$
��1811��$��6,10,0,0q�$��45��$	��Top=�.+��� -   
 ��Name$
 btnStart�����	#FF2D5774$��18(�$
��Bold"�$��5q�$	��120��
�������.��START�� -   $
 	btnBypass�����	#FF2D5774$��18(�$
��Bold"�$��5q�$	��120��
�������.��BYPASS�� -   $
 	btnRecord�����	#FF2D5774$��18(�$
��Bold"�$��5q�$	��120��
�������.��RECORD�� #��["�$��	25,5,25,5q��� -   $
 btnProcessPallet�����	#FF2D5774$��18(�$
��Bold"�$��5q�$	��180��
�������.��PROCESS PALLET�� -   $
 btnProcessRecording�����	#FF2D5774$��18(�$
��Bold"�$��5q�$	��220��
�������.��PROCESS RECORDING�� #��["�$��	25,5,25,5q��� -   $
 btnSettingsControl�����	#FF2D5774$��18(�$
��Bold"�$��5q�$	��120��
�������.��
SETTINGS�� -   $
 btnStatusControl�����	#FF2D5774$��18(�$
��Bold"�$��5q�$	��120��
�������.��STATUS�� -	    ��Name$ CurDateTime��$��24(�$
��Bold"�
�������$��Center=�$��0,0,25,0q�$��Right=�1/1/2021 5:38pm� -
    �Name$ CBB_Container��$��1��$��3�����	#FF000508$��
10,14,0,23q��� -    ��Name$ CapList�� �� Orientation$ Vertical=�$��1��$��3��$��
0,30,0,-36q�� $��1��$��4��	 �� $
��211* ��� $
��181* �.+��� -    ��Name$ imgPassSymbol��$��313,103,249,319q�$��/Art/Check.png��$y�Visible=��� -   $ imgPassText��$��97,90,315,304q�$��/Art/Pass.png��$y�Visible=� J� RenderTransformOrigin$ -1.191,0.561&��� -   $ imgFailSymbol��$��318,110,254,324q�$��
/Art/X.png��$y�Visible=��� -   $ imgFailText��$��94,101,328,316q�$��/Art/Fail.png��$y�Visible=�$ -1.191,0.561&��� -   $
 
btnDefects�����	#FF2D5774$��0��$��18(�$
��Bold"�$��
0,155,50,0q�
�������$��Right=�$	��125��$��35��$	��Top=�.��	DEFECTS&   System.Windows.Controls.DataGrid  -   $��defectTable��
�������$��0��   AlternatingRowBackground
 ��ح� �� AlternationCount$ 2��   AutoGenerateColumns .  $��50,211,50,10q�$��Normal"�   ColumnHeaderHeight$ 30Q�   	RowHeight$ 30Q�   RowStyle ��  �� 
TargetType)  #System.Windows.Controls.DataGridRow"  .���  �� Property � �� �� Value$
 Bold"���  � ��$ 16(�   ColumnHeaderStyle �� =  7System.Windows.Controls.Primitives.DataGridColumnHeader"  .���  � ��$
 Bold"���  � ��$ 16(�   Columns 0  *System.Windows.Controls.DataGridTextColumn  ,  &System.Windows.Controls.DataGridColumn   Header$ Board��   Width5  /System.Windows.Controls.DataGridLengthConverter$ 75    
IsReadOnly . 1  +System.Windows.Controls.DataGridBoundColumn   Binding �� *
Location+  $ Defect��$	 100  .  �� *Code+  $ Comment��$ *  .  �� *	Comment+�� -   $
 btnStatistics�����	#FF2D5774$��1��$��18(�$
��Bold"�$��	0,35,50,0q�
�������$��Right=�$	��125��$��35��$	��Top=�.��
STATISTICS  -   $��statisticsTable��
�������$��1��
 ��ح�$ 2�� .  $��
50,90,50,0q�$	��340��$	��Top=� �� "  .���  � ��$
 Bold"���  � ��$ 16(� �� "  .���  � ��$
 Bold"���  � ��$ 16(�   $  ��$	 150  .   $ Count��$ 75  .   $ %��$ 75  .   $ Count��$ 75  .   $ %��$ 75  . 