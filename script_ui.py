import re

file_path = r'c:\Users\Simps\.continue\Новая папка\KtpnConfigurator_Final\src\KtpnConfigurator.App\MainWindow.xaml'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace the top Toolbar Border with materialDesign:ColorZone
toolbar_pattern = r'<Border DockPanel\.Dock="Top" Background="{DynamicResource PrimaryHueMidBrush}" Padding="16,12">(.+?)</Border>'
new_toolbar = r'<materialDesign:ColorZone Mode="PrimaryMid" DockPanel.Dock="Top" Padding="16,12" materialDesign:ElevationAssist.Elevation="Dp4" Panel.ZIndex="1">\1</materialDesign:ColorZone>'
content = re.sub(toolbar_pattern, new_toolbar, content, flags=re.DOTALL)

# Add icons to Buttons in Toolbar (replace text with StackPanel + Icon)
def replace_button(text, icon, tooltip, flat=True):
    global content
    style = '{StaticResource MaterialDesignFlatLightBgButton}' if flat else '{StaticResource MaterialDesignRaisedButton}'
    old_btn = f'Content="{text}" Command="{{Binding '
    
    if text == "Новый": cmd = "NewProjectCommand}"
    elif text == "Открыть…": cmd = "OpenProjectCommand}"
    elif text == "Сохранить…": cmd = "SaveProjectCommand}"
    elif text == "Экспорт в Excel": cmd = "ExportExcelCommand}"
    elif text == "Экспорт в PDF": cmd = "ExportPdfCommand}"
    else: cmd = ""

    old_full = f'<Button Content="{text}" Command="{{Binding {cmd}" Style="{{StaticResource MaterialDesignRaisedButton}}"'
    new_full = f'''<Button ToolTip="{tooltip}" Command="{{Binding {cmd}" Style="{style}"'''
    
    content = content.replace(old_full, new_full)
    
    # We also need to add the icon content inside the button instead of Content property.
    # Actually, if we remove Content="...", we need to inject the content.
    # The existing buttons are self-closing or not? Let's check the original read:
    # <Button Content="Новый" Command="{Binding NewProjectCommand}" Style="{StaticResource MaterialDesignRaisedButton}" Margin="0,0,8,0"/>
    
    search_str = f'<Button ToolTip="{tooltip}" Command="{{Binding {cmd}" Style="{style}" Margin="0,0,8,0"'
    
    # We will use regex to replace the self-closing button with a button containing a StackPanel
    pattern = rf'<Button Content="{text}"([^>]+)/>'
    replacement = f'''<Button\\1 ToolTip="{tooltip}">
                        <StackPanel Orientation="Horizontal">
                            <materialDesign:PackIcon Kind="{icon}" Margin="0,0,6,0" VerticalAlignment="Center"/>
                            <TextBlock Text="{text}" VerticalAlignment="Center"/>
                        </StackPanel>
                    </Button>'''
    content = re.sub(pattern, replacement, content)

replace_button("Новый", "FileOutline", "Новый проект", flat=True)
replace_button("Открыть…", "FolderOpenOutline", "Открыть проект", flat=True)
replace_button("Сохранить…", "ContentSaveOutline", "Сохранить проект", flat=True)

# For Excel and PDF, we have Background attributes
content = re.sub(r'<Button Content="Экспорт в Excel"([^>]+)/>', 
                 r'''<Button\1 ToolTip="Экспорт в Excel">
                        <StackPanel Orientation="Horizontal">
                            <materialDesign:PackIcon Kind="FileExcelBox" Margin="0,0,6,0" VerticalAlignment="Center"/>
                            <TextBlock Text="Excel" VerticalAlignment="Center"/>
                        </StackPanel>
                    </Button>''', content)

content = re.sub(r'<Button Content="Экспорт в PDF"([^>]+)/>', 
                 r'''<Button\1 ToolTip="Экспорт в PDF">
                        <StackPanel Orientation="Horizontal">
                            <materialDesign:PackIcon Kind="FilePdfBox" Margin="0,0,6,0" VerticalAlignment="Center"/>
                            <TextBlock Text="PDF" VerticalAlignment="Center"/>
                        </StackPanel>
                    </Button>''', content)


# Tab Icons (TabControl in MaterialDesignThemes usually uses Header as simple content, but we can put a StackPanel in Header)
def replace_tab_header(text, icon):
    global content
    pattern = rf'<TabItem Header="{text}">'
    replacement = f'''<TabItem>
                <TabItem.Header>
                    <StackPanel Orientation="Horizontal">
                        <materialDesign:PackIcon Kind="{icon}" Margin="0,0,8,0" VerticalAlignment="Center"/>
                        <TextBlock Text="{text}" VerticalAlignment="Center"/>
                    </StackPanel>
                </TabItem.Header>'''
    content = content.replace(pattern, replacement)

replace_tab_header("1. Трансформатор и корпус", "Transformer")
replace_tab_header("2. Ввод РУВН", "LightningBolt")
replace_tab_header("3. Ввод и линии РУНН", "PowerPlug")
replace_tab_header("4. Результат расчёта", "CalculatorVariant")
replace_tab_header("5. Документы", "FileDocumentMultipleOutline")

# Fix Flat Buttons in toolbar replacing RaisedButton
content = content.replace('Command="{Binding NewProjectCommand}" Style="{StaticResource MaterialDesignRaisedButton}"', 'Command="{Binding NewProjectCommand}" Style="{StaticResource MaterialDesignFlatLightBgButton}"')
content = content.replace('Command="{Binding OpenProjectCommand}" Style="{StaticResource MaterialDesignRaisedButton}"', 'Command="{Binding OpenProjectCommand}" Style="{StaticResource MaterialDesignFlatLightBgButton}"')
content = content.replace('Command="{Binding SaveProjectCommand}" Style="{StaticResource MaterialDesignRaisedButton}"', 'Command="{Binding SaveProjectCommand}" Style="{StaticResource MaterialDesignFlatLightBgButton}"')

# Also for Tab 5 export buttons
content = re.sub(r'<Button Content="Экспорт всех в Excel"([^>]+)/>', 
                 r'''<Button\1 ToolTip="Экспорт всех документов">
                        <StackPanel Orientation="Horizontal">
                            <materialDesign:PackIcon Kind="FileExcelBox" Margin="0,0,6,0" VerticalAlignment="Center"/>
                            <TextBlock Text="Экспорт всех в Excel" VerticalAlignment="Center"/>
                        </StackPanel>
                    </Button>''', content)
content = re.sub(r'<Button Content="Экспорт всех в PDF"([^>]+)/>', 
                 r'''<Button\1 ToolTip="Экспорт всех документов">
                        <StackPanel Orientation="Horizontal">
                            <materialDesign:PackIcon Kind="FilePdfBox" Margin="0,0,6,0" VerticalAlignment="Center"/>
                            <TextBlock Text="Экспорт всех в PDF" VerticalAlignment="Center"/>
                        </StackPanel>
                    </Button>''', content)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)
print("Updated MainWindow.xaml")
