using DiskSpaceDisplay_PoC.Logic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DiskSpaceDisplay_PoC
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private int InfoUnitMultiplier = 1000;
		private long BlockSize;
		private double BlockSpacing = 2.0;
		private Brush FilledColor = Brushes.Green;
		private Brush EmptyColor = Brushes.LightGray;
		private Brush BackgroundColor = new SolidColorBrush(Color.FromRgb(234,242,246));
		private Color ShadowColor = Color.FromRgb(129, 163, 202);
		private bool Initialized = false; //: Are all UI elements (components) exist now


		public MainWindow()
		{
			InitializeComponent();
			//: Init settings
			InfoUnitMultiplier = int.Parse(((ComboBoxItem)coUnitSelector.SelectedItem).Tag.ToString());
			BlockSize = GetBlockSize();
#if !DEBUG
			Init();
#endif
			Initialized = true;
			this.SizeToContent = SizeToContent.Width;
		}

		#region Logic Functions

		private void UpdateUI()
		{
			GetDrivesInfo();
			// Update settings
			BlockSize = GetBlockSize();
			InfoUnitMultiplier = int.Parse(((ComboBoxItem)coUnitSelector.SelectedItem).Tag.ToString());

			// Clear existing disk panels
			spDiskPanel.Children.Clear();

			// Reload drives with new settings
			RenderDrives();
		}

		private void UpdateUIAuto()
		{
			if (!Initialized) return; //: костыль for event being triggered before completing InitializeComponent
			if (cbAutoUpdate.IsChecked == true)
				UpdateUI();
		}

		private List<DriveInfo> GetDrivesInfo()
		{
			List<DriveInfo> drives = new();
			foreach (DriveInfo drive in DriveInfo.GetDrives())
			{
				drives.Add(drive);
			}
			return drives;
		}

		private void RenderDrives()
		{
			List<DriveInfo> drives = GetDrivesInfo();
			string DiskInfoVariant = ((ComboBoxItem)coDiskInfo.SelectedItem).Tag.ToString();
			string BlockCut =  ((ComboBoxItem)coBlockCut.SelectedItem).Tag.ToString();
			string Style = ((ComboBoxItem)coLineStyle.SelectedItem).Tag.ToString();


			foreach (var drive in drives)
			{
				if (!drive.IsReady) continue;
				long occupiedSpace = drive.TotalSize - drive.TotalFreeSpace;

				StackPanel drivePanel = new StackPanel
				{
					Margin = new Thickness(5),
					Background = BackgroundColor,
					Effect = new DropShadowEffect
					{
						Color = ShadowColor,
						BlurRadius = 5,
						ShadowDepth = 2
					}
				};
				//: drive name and label TextBlock
				drivePanel.Children.Add(new TextBlock { Text = $"{drive.Name} ({drive.VolumeLabel})", FontWeight = FontWeights.Bold });
				//: drive info TextBlock
				if (DiskInfoVariant != "none")
					drivePanel.Children.Add(new TextBlock { Text = $"Total: {drive.TotalSize / UnitToMultiplyer("GB")}"
														+ (DiskInfoVariant == "all"? $"GB, Used: {(occupiedSpace) / UnitToMultiplyer("GB")} ({(((double)occupiedSpace/drive.TotalSize)*100).ToString()[..5]}%) Free: {drive.TotalFreeSpace / UnitToMultiplyer("GB")} GB"
														:"")});

				Grid barBlocksGrid = new Grid { Background = BackgroundColor, Height = 10 }; //. Grid for blocks bar
				Grid barLineGrid = new Grid { Background = BackgroundColor, Height = 10 }; //. Grid for line

				long totalBlocks = (int)Math.Ceiling((double)drive.TotalSize/BlockSize);
				// partial cutted block either from the start, either from the end (BlockCut setting)
				double outermostBlockFill = (double)(drive.TotalSize%BlockSize)/BlockSize;

				//TODO: Recalculate for BlockCut = "start"
				//: partial number of used blocks
				double usedBlocks = (double)(occupiedSpace) / BlockSize; 
				//: Whole number of used blocks
				long fullUsedBlocks = BlockCut == "end"? (long)(usedBlocks)
													   : (usedBlocks < outermostBlockFill? 0 //. if even first block is not full, no blocks are full
																						 : 1 + (long)(usedBlocks-outermostBlockFill)) ; //. add first filled partial block, add other blocks minus this partial block length 
				double partialFill = totalBlocks == 1 ? (double)occupiedSpace / drive.TotalSize //. if it's the only block, fill it for occupied space
													  : BlockCut == "end" ? usedBlocks - fullUsedBlocks
																	      : 1 - (fullUsedBlocks - usedBlocks) - outermostBlockFill;

				
				spDiskPanel.Children.Add(drivePanel);

				if (Style.Contains("block"))
				{
					
					drivePanel.Children.Add(barBlocksGrid);
					for (int i = 0; i < totalBlocks; i++)
					{
						ColumnDefinition column = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };
						barBlocksGrid.ColumnDefinitions.Add(column);
					}

					for (int i = 0; i < totalBlocks; i++)
					{
						if (i < fullUsedBlocks)
						{
							Rectangle rect = new Rectangle
							{
								Fill = FilledColor,
								Margin = new Thickness(BlockSpacing / 2, 0, BlockSpacing / 2, 0)
							};
							Grid.SetColumn(rect, i);
							barBlocksGrid.Children.Add(rect);
						}
						else if (i == fullUsedBlocks && partialFill > 0)
						{
							Grid partialBlock = new Grid { Margin = new Thickness(BlockSpacing / 2, 0, BlockSpacing / 2, 0) };
							partialBlock.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(partialFill, GridUnitType.Star) });
							partialBlock.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1 - partialFill, GridUnitType.Star) });
							Rectangle filledPart = new Rectangle { Fill = FilledColor, HorizontalAlignment = HorizontalAlignment.Stretch };
							Rectangle emptyPart = new Rectangle { Fill = EmptyColor, HorizontalAlignment = HorizontalAlignment.Stretch };
							Grid.SetColumn(filledPart, 0);
							Grid.SetColumn(emptyPart, 1);
							partialBlock.Children.Add(filledPart);
							partialBlock.Children.Add(emptyPart);
							Grid.SetColumn(partialBlock, i);
							barBlocksGrid.Children.Add(partialBlock);
						}
						else
						{
							Rectangle rect = new Rectangle
							{
								Fill = EmptyColor,
								Margin = new Thickness(BlockSpacing / 2, 0, BlockSpacing / 2, 0)
							};
							Grid.SetColumn(rect, i);
							barBlocksGrid.Children.Add(rect);
						}

						if (i == totalBlocks - 1 && i != 0) //: Last block
						{
							barBlocksGrid.UpdateLayout();
							var lastBlockLength = new GridLength(barBlocksGrid.ColumnDefinitions.First().ActualWidth * outermostBlockFill, GridUnitType.Pixel);
							if (BlockCut == "start")
								barBlocksGrid.ColumnDefinitions.First().Width = lastBlockLength;
							if (BlockCut == "end")
								barBlocksGrid.ColumnDefinitions.Last().Width = lastBlockLength;
						}
					}
				}

				if (Style.Contains("line")) {
					
					barLineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(occupiedSpace, GridUnitType.Star) });
					barLineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(drive.TotalFreeSpace , GridUnitType.Star) });
					Rectangle filledPart = new Rectangle { Fill = FilledColor, HorizontalAlignment = HorizontalAlignment.Stretch };
					Rectangle emptyPart = new Rectangle { Fill = EmptyColor, HorizontalAlignment = HorizontalAlignment.Stretch };
					Grid.SetColumn(filledPart, 0);
					Grid.SetColumn(emptyPart, 1);
					barLineGrid.Children.Add(filledPart);
					barLineGrid.Children.Add(emptyPart);

					drivePanel.Children.Add(barLineGrid);
				}
			}
		}

		/// <summary>
		/// Block size in bytes
		/// </summary>
		/// <returns></returns>
		private long GetBlockSize()
		{
			if (long.TryParse(tbBlockSizeInput.Text, out long size))
			{
				string unit = ((ComboBoxItem)coBlockSizeUnitSelector.SelectedItem).Tag.ToString();
				return size * UnitToMultiplyer(unit);
			}
			//: If parse fails, reset Combobox and textbox to default values
			tbBlockSizeInput.Text = "100";
			coBlockSizeUnitSelector.SelectedIndex = 3;

			return 100 * UnitToMultiplyer("GB"); // Default to 100 GB if parsing fails
		}

		public long UnitToMultiplyer(string unit)
		{
			 return unit.ToUpper() switch
			 {
				"KB" => InfoUnitMultiplier,
				"MB" => InfoUnitMultiplier * InfoUnitMultiplier,
				"GB" => InfoUnitMultiplier * InfoUnitMultiplier * InfoUnitMultiplier,
				"TB" => InfoUnitMultiplier * InfoUnitMultiplier * InfoUnitMultiplier * InfoUnitMultiplier,
				 _ => 1,
			 };
		}

		#endregion

		#region Event Handlers
		private void UpdateButton_Click(object sender, RoutedEventArgs e)
		{
			UpdateUI();
		}
		#endregion

		private void UnitSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateUIAuto();
		}

		private void BlockSizeUnitSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateUIAuto();
		}

		private void BlockSizeInput_LostFocus(object sender, RoutedEventArgs e)
		{
			UpdateUIAuto();
		}
	}


}