using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Titanium;

namespace DiskSpaceDisplay_PoC
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private int InfoUnitMultiplier = 1000;
		private long BlockSize;
		private int ZebraDivisor = 10;
		private double BlockSpacing = 2.0;

		private Brush FilledColor = Brushes.Green;
		private Brush FilledZebraColor = new SolidColorBrush(Color.FromRgb(0,144,0));

		private Brush EmptyColor = new SolidColorBrush(Color.FromRgb(120,110,110));
		private Brush EmptyZebraColor = new SolidColorBrush(Color.FromRgb(140,125,125));

		private Brush BackgroundColor = new SolidColorBrush(Color.FromRgb(234,242,246));
		private Color ShadowColor = Color.FromRgb(129, 163, 202);
		private bool Initialized = false; //: Are all UI elements (components) exist now
		private bool MimicAtivated = false;
		private int MimicActivationCounter = 10; //: Times to click secret button to activate mimic drive

		//: Mimic drive (for testing interface)
		private MimicDriveInfo? MimicDrive = null;
		private class MimicDriveInfo
		{
			public string VolumeLabel;
			public string Name;
			public long TotalSize;
			public long TotalFreeSpace;
			public bool IsReady;
			public readonly bool IsMimic = false; //: is editable artificial drive

			public MimicDriveInfo(string VolumeLabel, string Name, long totalSize, long totalFreeSpace)
			{
				VolumeLabel = VolumeLabel;
				Name = Name;
				TotalSize = totalSize;
				TotalFreeSpace = totalFreeSpace;
				IsReady = true;
			}

			public MimicDriveInfo()
			{
				IsReady = false;
			}

			public MimicDriveInfo(DriveInfo driveInfo)
			{
				VolumeLabel = driveInfo.VolumeLabel;
				Name = driveInfo.Name;
				TotalSize = driveInfo.TotalSize;
				TotalFreeSpace = driveInfo.TotalFreeSpace;
				IsReady = driveInfo.IsReady;
			}

			public MimicDriveInfo(MimicDriveInfo driveInfo)
			{
				VolumeLabel = driveInfo.VolumeLabel;
				Name = driveInfo.Name;
				TotalSize = driveInfo.TotalSize;
				TotalFreeSpace = driveInfo.TotalFreeSpace;
				IsReady = driveInfo.IsReady;
				IsMimic = true;
			}
		}




		public MainWindow()
		{
			InitializeComponent();
			//: Init settings
			InfoUnitMultiplier = int.Parse(((ComboBoxItem)coUnitSelector.SelectedItem).Tag.ToString());
			BlockSize = GetBlockSize();
#if !DEBUG
			UpdateUI();
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
			if (!Initialized) return; //: костыль-fix for event being triggered before completing InitializeComponent
			if (cbAutoUpdate.IsChecked == true)
				UpdateUI();
		}

		private List<MimicDriveInfo> GetDrivesInfo()
		{
			List<MimicDriveInfo> drives = new();
			foreach (DriveInfo drive in DriveInfo.GetDrives())
			{
				drives.Add(new MimicDriveInfo(drive));
			}

			
			if (!MimicAtivated) return drives;
			//: Add mimic drive if it's activated
			MimicDrive ??= new MimicDriveInfo(drives[Random.Shared.Next(0, drives.Count - 1)]);
			drives.Add(MimicDrive);
			return drives;
		}

		private void RenderDrives()
		{
			List<MimicDriveInfo> drives = GetDrivesInfo();
			string DiskInfoVariant = ((ComboBoxItem)coDiskInfo.SelectedItem).Tag.ToString();
			string BlockCut =  ((ComboBoxItem)coBlockCut.SelectedItem).Tag.ToString();
			string Style = ((ComboBoxItem)coLineStyle.SelectedItem).Tag.ToString();
			string UnitMultiplicity = ((ComboBoxItem)coBlockSizeUnitSelector.SelectedItem).Tag.ToString();


			foreach (var drive in drives)
			{
				if (!drive.IsReady) continue;
				long occupiedSpace = drive.TotalSize - drive.TotalFreeSpace;

				Grid driveGrid = new Grid();
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
				var driveNameAndLabelLB = new TextBlock { Text = $"{drive.Name} ({drive.VolumeLabel})", FontWeight = FontWeights.Bold };
				drivePanel.Children.Add(driveNameAndLabelLB);
				//: drive info TextBlock
				if (drive.IsMimic == false)
				{
					if (DiskInfoVariant != "none")
						drivePanel.Children.Add(new TextBlock
						{
							Text = $"Total: {drive.TotalSize / UnitToMultiplyer("GB")}"
							       + (DiskInfoVariant == "all"
								       ? $"GB, Used: {(occupiedSpace) / UnitToMultiplyer("GB")} ({(((double)occupiedSpace / drive.TotalSize) * 100).ToString()[..5]}%) Free: {drive.TotalFreeSpace / UnitToMultiplyer("GB")} GB"
								       : "")
						});
				}
				else //? mimic drive info TextBlock, editable on click
				{
					//. Show all info regardless of settings
					var mimicTextBlock = new TextBlock
					{
						Text = $"Total: {drive.TotalSize / UnitToMultiplyer("GB")} GB, Used: {(occupiedSpace) / UnitToMultiplyer("GB")} ({(((double)occupiedSpace / drive.TotalSize) * 100).ToString("F2")}%) Free: {drive.TotalFreeSpace / UnitToMultiplyer("GB")} GB"
					};
					drivePanel.Children.Add(mimicTextBlock);
					//. Add an event on TextBlock click – replace all info with editable fields
					mimicTextBlock.MouseLeftButtonDown += (sender, args) =>
					{
						//. Remove TextBlock
						drivePanel.Children.Remove(mimicTextBlock);
						drivePanel.Children.Remove(driveNameAndLabelLB);
						//. Add editable fields
						StackPanel MimicInfoSP = new StackPanel {Orientation= Orientation.Horizontal};

						TextBox tbName = new TextBox { Text = drive.Name, FontWeight = FontWeights.Bold };
						TextBox tbVolumeLabel = new TextBox { Text = drive.VolumeLabel, FontWeight = FontWeights.Bold  };
						TextBlock lbTotalSize = new TextBlock { Text = "Total Size: ", Margin = new Thickness(10, 0, 3, 0) };
						TextBox tbTotalSize = new TextBox { Text = (drive.TotalSize / UnitToMultiplyer(UnitMultiplicity)).ToString() };
						TextBlock lbTotalSizeUnit = new TextBlock { Text = UnitMultiplicity, Margin = new Thickness(2, 0, 0, 0) };
						TextBlock lbTotalFreeSpace = new TextBlock { Text = "Total Free Space: ", Margin = new Thickness(10, 0, 3, 0) };
						TextBox tbTotalFreeSpace = new TextBox { Text = (drive.TotalFreeSpace / UnitToMultiplyer(UnitMultiplicity)).ToString() };
						TextBlock lbTotalFreeSpaceUnit = new TextBlock { Text = UnitMultiplicity, Margin = new Thickness(2, 0, 0, 0) };
						Button saveButton = new Button { Content = "Save", Margin = new Thickness(10, 0, 3, 0)  };
						saveButton.Click += (sender, args) =>
						{
							drive.VolumeLabel = tbVolumeLabel.Text;
							drive.Name = tbName.Text;

							double? totalSizeLabel = tbTotalSize.Text.ToDoubleT(ExceptionValue: Double.NaN, CanBeNegative: false);
							if (totalSizeLabel == Double.NaN) //? parsing failed
								tbTotalSize.BorderBrush = Brushes.Red;
							else
								tbTotalSize.Text = totalSizeLabel.ToString(); //. place filtered by ToDoubleT value

							drive.TotalSize = (long)(totalSizeLabel * UnitToMultiplyer("GB"));

							double? totalFreeSpaceLabel = tbTotalFreeSpace.Text.ToDoubleT(ExceptionValue: Double.NaN, CanBeNegative:false);
							if (totalFreeSpaceLabel == Double.NaN) //? parsing failed
								tbTotalFreeSpace.BorderBrush = Brushes.Red;
							else
								tbTotalSize.Text = totalFreeSpaceLabel.ToString(); //. place filtered by ToDoubleT value

							drive.TotalFreeSpace = Math.Min(drive.TotalSize, (long)(totalFreeSpaceLabel * UnitToMultiplyer("GB"))); //. Don't let FreeSpace be more than TotalSize
							//. Remove editable fields
							drivePanel.Children.Remove(MimicInfoSP);
							UpdateUI(); 
						};
						MimicInfoSP.Children.Add(tbName);
						MimicInfoSP.Children.Add(tbVolumeLabel);
						MimicInfoSP.Children.Add(lbTotalSize);
						MimicInfoSP.Children.Add(tbTotalSize);
						MimicInfoSP.Children.Add(lbTotalSizeUnit);
						MimicInfoSP.Children.Add(lbTotalFreeSpace);
						MimicInfoSP.Children.Add(tbTotalFreeSpace);
						MimicInfoSP.Children.Add(lbTotalFreeSpaceUnit);
						MimicInfoSP.Children.Add(saveButton);
						drivePanel.Children.Insert(0,MimicInfoSP);
					};

				}

				driveGrid.Children.Add(drivePanel);
				spDiskPanel.Children.Add(driveGrid);


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

				

				if (Style.Contains("block"))
				{
					bool PerviousZebra = true; //: was the last mini-block in the pervious block (ZebraGrid) zebra? 
					
					drivePanel.Children.Add(barBlocksGrid);
					for (int i = 0; i < totalBlocks; i++)
					{
						ColumnDefinition column = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };
						barBlocksGrid.ColumnDefinitions.Add(column);
					}

					for (int bl = 0; bl < totalBlocks; bl++) //! Blocks cycle
					{
						bool isBlockFilled = (bl < fullUsedBlocks);
						bool isLastBlock = bl == totalBlocks - 1 && bl != 0;

						if (bl == fullUsedBlocks && partialFill > 0) //? Partial block
						{
							if (Style.Contains("zebra"))
							{
								int fullMiniBlocks = (int)(partialFill * ZebraDivisor);
								double partialMiniBlockFill = (partialFill * ZebraDivisor) - fullMiniBlocks;

								Grid partialBlock = new Grid { Margin = new Thickness(BlockSpacing / 2, 0, BlockSpacing / 2, 0) };
								for (int mbl = 0; mbl < ZebraDivisor; mbl++) { //! Miniblocks cycle
									bool miniblockIsFilled = (mbl < fullMiniBlocks);
									if (mbl == fullMiniBlocks) { //? Partial miniblock
										Grid partialMiniblock = new Grid {  };
										partialMiniblock.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(partialMiniBlockFill, GridUnitType.Star) });
										partialMiniblock.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1 - partialMiniBlockFill, GridUnitType.Star) });

										bool currentMiniblockIsZebra = (mbl % 2 == (PerviousZebra ? 0 : 1));
										var filledMiniblock = new Rectangle
										{
											Fill = currentMiniblockIsZebra ? FilledColor : FilledZebraColor,
											HorizontalAlignment = HorizontalAlignment.Stretch
										};

										var emptyMiniblock = new Rectangle
										{
											Fill = currentMiniblockIsZebra ? EmptyColor : EmptyZebraColor,
											HorizontalAlignment = HorizontalAlignment.Stretch
										};

										Grid.SetColumn(filledMiniblock, 0);
										Grid.SetColumn(emptyMiniblock, 1);
										partialMiniblock.Children.Add(filledMiniblock);
										partialMiniblock.Children.Add(emptyMiniblock);
										Grid.SetColumn(partialMiniblock, mbl);
										partialBlock.Children.Add(partialMiniblock);
									} else { //? Whole miniblock
										partialBlock.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
										bool currentMiniblockIsZebra = (mbl % 2 == (PerviousZebra ? 0 : 1));
										var miniblock = new Rectangle
										{
											Fill = miniblockIsFilled ? (currentMiniblockIsZebra ? FilledColor : FilledZebraColor)
												: (currentMiniblockIsZebra ? EmptyColor : EmptyZebraColor),
											HorizontalAlignment = HorizontalAlignment.Stretch
										};
										Grid.SetColumn(miniblock, mbl);
										partialBlock.Children.Add(miniblock);
									}
								}
								PerviousZebra = ZebraDivisor % 2 == 0 ? PerviousZebra : !PerviousZebra; //? if ZebraDivisor is odd, first miniblock will vary zebra every block
								Grid.SetColumn(partialBlock, bl);
								barBlocksGrid.Children.Add(partialBlock);
							}
							else //? Not zebra
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
								Grid.SetColumn(partialBlock, bl);
								barBlocksGrid.Children.Add(partialBlock);
							}
						}
						else //? Whole block
						{
							if (Style.Contains("zebra"))
							{
								Grid ZebraGrid = new Grid { Margin = new Thickness(BlockSpacing / 2, 0, BlockSpacing / 2, 0) };
								for (int j = 0; j < ZebraDivisor; j++)
								{
									var column = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };
									ZebraGrid.ColumnDefinitions.Add(column);
									bool currentMiniblockIsZebra = (j % 2 == (PerviousZebra ? 0 : 1));

									var miniblock = new Rectangle
									{
										Fill = isBlockFilled? (currentMiniblockIsZebra ? FilledColor : FilledZebraColor)
															: (currentMiniblockIsZebra ? EmptyColor : EmptyZebraColor),
										HorizontalAlignment = HorizontalAlignment.Stretch
									};
									Grid.SetColumn(miniblock, j);
									ZebraGrid.Children.Add(miniblock);
								}
								PerviousZebra = ZebraDivisor%2 == 0? PerviousZebra : !PerviousZebra; //? if ZebraDivisor is odd, first miniblock will vary zebra every block
								Grid.SetColumn(ZebraGrid, bl);
								barBlocksGrid.Children.Add(ZebraGrid);
							}
							else
							{
								Rectangle block = new Rectangle
								{
									Fill = isBlockFilled? FilledColor : EmptyColor,
									Margin = new Thickness(BlockSpacing / 2, 0, BlockSpacing / 2, 0)
								};
								Grid.SetColumn(block, bl);
								barBlocksGrid.Children.Add(block);
							}

							
						}

						if (isLastBlock) //? Last block
						{ //TODO: fix for Zebra blocks
							ColumnDefinition lastBlockColumnDefinition = new ColumnDefinition { Width = new GridLength(outermostBlockFill, GridUnitType.Star) };
							if (BlockCut == "start")
								barBlocksGrid.ColumnDefinitions[0] = lastBlockColumnDefinition;
							if (BlockCut == "end")
								barBlocksGrid.ColumnDefinitions[^1] = lastBlockColumnDefinition;
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
				if (drive.IsMimic && MimicActivationCounter == 0) //: Add appear animation to mimic drive
				{
					spDiskPanel.UpdateLayout();
					drivePanel.UpdateLayout();
					int MaxWidth = (int)drivePanel.ActualWidth;
					int MaxHeight = (int)drivePanel.ActualHeight;
					int MaxMinDimention = Math.Min(MaxWidth, MaxHeight);

					//: Black gradient panel
					Grid blackGradientPanel = new Grid
					{
						Opacity = 1.0,
						Height = MaxHeight,
						Width = MaxWidth
					};

					//: Add a Canvas to hold the circles
					Canvas circleCanvas = new Canvas
					{
						Width = MaxWidth,
						Height = MaxHeight
					};

					//: Add random circle gradients
					Random random = new Random();
					for (int i = 0; i < 300; i++) {
						byte RandomFillBrightness = (byte)random.Next(0, 35);
						Ellipse circle = new Ellipse
						{
							Width = random.Next(50, MaxMinDimention * 3),
							Height = random.Next(50, MaxMinDimention * 3),
							Fill = new RadialGradientBrush(Color.FromRgb(RandomFillBrightness, RandomFillBrightness, RandomFillBrightness), Color.FromArgb(0, 0, 0, 0))
						};
						Canvas.SetLeft(circle, random.Next(-MaxMinDimention / 2, (int)(MaxWidth + MaxMinDimention / 2)));
						Canvas.SetTop(circle, random.Next(-MaxMinDimention / 3, (int)(MaxHeight + MaxMinDimention / 3)));
						circleCanvas.Children.Add(circle);

						// Create and start the animation to make the circle slowly disappear
						DoubleAnimation fadeOutAnimation = new DoubleAnimation
						{
							From = 1.0,
							To = 0.0,
							Duration = new Duration(TimeSpan.FromSeconds(random.Next(1, 5))) // Random duration between 1 and 5 seconds
						};
						circle.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
					}

					//: Add the Canvas to the black gradient panel
					blackGradientPanel.Children.Add(circleCanvas);

					//: Create a new Grid to hold the last element and the black gradient panel
					driveGrid.Children.Add(blackGradientPanel);

					// Create and start the animation to make the black gradient panel slowly disappear
					DoubleAnimation fadeOutAnimationPanel = new DoubleAnimation
					{
						From = 1.0,
						To = 0.0,
						Duration = new Duration(TimeSpan.FromSeconds(1))
					};
					blackGradientPanel.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimationPanel);
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

		private System.Windows.Threading.DispatcherTimer? mimicResetTimer;
		public void MimicEasterEgg()
		{
			//: Add 3 sec timer that resets MimicActivationCounter to 10
			if (mimicResetTimer == null)
			{
				mimicResetTimer = new System.Windows.Threading.DispatcherTimer
				{
					Interval = TimeSpan.FromSeconds(3)
				};
				mimicResetTimer.Tick += (s, args) =>
				{
					if (MimicAtivated) return;
					MimicActivationCounter = 10;
					mimicResetTimer.Stop();
				};
			}

			mimicResetTimer.Stop();
			mimicResetTimer.Start();
			MimicActivationCounter--;
			if (MimicActivationCounter == 0) {
				MimicAtivated = true;
				UpdateUI();
			}
		}

		#endregion

		#region Event Handlers


		private void UpdateButton_Click(object sender, RoutedEventArgs e)
		{
			UpdateUI();
		}


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
		#endregion

		private void cbAutoUpdate_Checked(object sender, RoutedEventArgs e)
		{
			MimicEasterEgg();
		}
	}


}