   M S B A M L   `   `   `  ���� F  BPalletCheck, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null)clr-namespace:PalletCheckPalletCheck  ;"clr-namespace:PalletCheck.ControlsPalletCheck.Controls      PalletCheck.MainWindow   U QScottPlot.WPF, Version=4.1.27.0, Culture=neutral, PublicKeyToken=e53b06131e34a3aaR NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     +localclr-namespace:PalletCheck     7controls"clr-namespace:PalletCheck.Controls     -     � Title$  PalletCheck��$
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
�������.��STATUS�� -	    ��Name$ 
ModeStatus��$��32(�$
��Bold"�
�������$��0,0,25,0q� �� TextAlignment$ Center=�$	��319��IDLING�� -
   $ CurDateTime��$��24(�$
��Bold"�
�������$��Center=�$��Right=�$��0,0,25,0q�11/11/2021 15:38pm� -    �Name$ CBB_Container��$��1��$��3�����	#FF000508$��	10,14,0,0q�$	��941��$	��Top=��� -   $ 
PalletName��$��18(�$
��Bold"�
�������$��1��$#��0,25                    ,30,0q� K� ZIndex$	 100��$	��Top=�$��Right=� J� Opacity$	 0.5Q�$��3���� $&� XXXXXXXXXX Pallet Name XXXXXXXXX���� -   $ AppName��$��18(�$
��Bold"�
�������$��1��$!��25,25                  ,0,0q�$	 100��$	��Top=�$
��Left=�$	 0.5Q�$��3���� $�Multi Camera System - UI���� -    ��Name$ CBB_Button_List�� �� Orientation$ Vertical=�$��1��$��3��$��
0,30,0,-36q�� $��1��$��4��	 �� $
��475* ��� $
��509* �.+��� -    ��Name$ imgPassSymbol��$��
313,50,0,0q�$��/Art/Check.png��$y�Visible=�$
��Left=�$��68��$	��108��$	��Top=��� -   $ imgPassText��$��	97,50,0,0q�$��/Art/Pass.png��$y�Visible=� J� RenderTransformOrigin$ -1.191,0.561&�$	��136��$	��Top=�$
��Left=�$	��218���� -   $ imgFailSymbol��$��
318,50,0,0q�$��
/Art/X.png��$y�Visible=�$��96��$	��Top=�$
��Left=�$��58���� -   $ imgFailText��$��	94,50,0,0q�$��/Art/Fail.png��$y�Visible=�$ -1.191,0.561&�$	��113��$	��Top=�$
��Left=�$	��208���� -   $
 
btnDefects�����	#FF2D5774$��0��$��18(�$
��Bold"�$��455,109,0,0q�
�������$
��Left=�$	��125��$��35��$	��Top=�.��	DEFECTS&   System.Windows.Controls.DataGrid  -   $��defectTable��
�������$��0��   AlternatingRowBackground
 ��ح� �� AlternationCount$ 2��   AutoGenerateColumns .  $��	0,152,0,0q�$��Normal"�   ColumnHeaderHeight$ 30Q�   	RowHeight$ 30Q�$��Center=�$	��530��$	��309��$	��Top=�   RowStyle ��  �� 
TargetType)  #System.Windows.Controls.DataGridRow"  .���  �� Property � �� �� Value$
 Bold"���  � ��$ 16(�   ColumnHeaderStyle �� =  7System.Windows.Controls.Primitives.DataGridColumnHeader"  .���  � ��$
 Bold"���  � ��$ 16(�   Columns 0  *System.Windows.Controls.DataGridTextColumn  ,  &System.Windows.Controls.DataGridColumn   Header$ Board��    Width5  /System.Windows.Controls.DataGridLengthConverter$  75 !   
IsReadOnly! . 1  +System.Windows.Controls.DataGridBoundColumn"   Binding" �� *
Location+  $ Defect��$	  100 ! . " �� *Code+  $ Comment��$  * ! . " �� *	Comment+�� -   $
 btnStatistics�����	#FF2D5774$��1��$��18(�$
��Bold"�$��
455,42,0,0q�
�������$
��Left=�$	��125��$��35��$	��Top=�.��CLEAR  -   $��statisticsTable��
�������$��1��
 ��ح�$ 2�� .  $��50,0,0,0q�$	��329��$��Center=�$
��Left=�$	��530�� �� "  .���  � ��$
 Bold"���  � ��$ 16(� �� "  .���  � ��$
 Bold"���  � ��$ 16(�   $ Description��$  * ! . " �� *Description+  $ Count��$  * ! . " �� *Count1+  $ %��$  * ! . " �� *
Percent1+� -   # �Name$# ProgressBar��$��Center=�$��46��$��	0,436,0,0q�$��1��$	��Top=�$	��530������ $ �� 
TargetType"$ �.R��� 
���   �$��3q�
�������._�� $ 
PART_Track��.+��� % ��Name$% PART_Indicator��$
��Left=����	#FF2D5774�� -   $ ProgressBarText��$��Center=�$��	0,448,0,0q�$��1��$
��Wrap=�$��Progress Bar Text��$	��Top=�
�������$
��Bold"�$��16(�